using Microsoft.Extensions.Logging;
using DrugExplorer.Application.Interfaces;
using DrugExplorer.Domain.Entities;
using DrugExplorer.Domain.Models;

namespace DrugExplorer.Application.Services;

public class MedicamentSeedService : IMedicamentSeedService
{
    private const int PAGE_SIZE = 100;
    private const int MAX_PAGES_PER_QUERY = 3;
    private const int MAX_TARGET_COUNT = 5000;
    private const int SAVE_BATCH_SIZE = 200;
    private const int MAX_VARIANTS_PER_GENERIC_NAME = 3;
    private static readonly TimeSpan DelayBetweenRequests = TimeSpan.FromMilliseconds(300);

    private readonly IOpenFdaClient _client;
    private readonly IMedicamentRepository _repository;
    private readonly ILogger<MedicamentSeedService> _logger;

    // Broad spectrum of generic drug names (analgesics, antibiotics, cardiovascular, CNS, endocrine,
    // GI, respiratory, dermatological, oncology, etc.) based on the WHO Model List of Essential
    // Medicines plus common US OTC/prescription generics, used to query OpenFDA's drug/label endpoint.
    private static readonly string[] SearchTerms =
    {
        // Completed: Analgesics / NSAIDs / migraine
        // "acetaminophen", "ibuprofen", "aspirin", "naproxen", "diclofenac", "ketorolac", "celecoxib",
        // "meloxicam", "indomethacin", "sumatriptan", "rizatriptan", "topiramate", "propranolol",

        // Completed: Opioids
        // "morphine", "codeine", "fentanyl", "oxycodone", "hydrocodone", "hydromorphone", "tramadol",
        // "methadone", "buprenorphine", "naloxone", "naltrexone",

        // Completed: Antibiotics
        // "amoxicillin", "amoxicillin clavulanate", "ampicillin", "penicillin", "cephalexin", "cefazolin",
        // "cefuroxime", "cefdinir", "ceftriaxone", "cefepime", "azithromycin", "clarithromycin",
        // "erythromycin", "doxycycline", "minocycline", "tetracycline", "ciprofloxacin", "levofloxacin",
        // "moxifloxacin", "clindamycin", "metronidazole", "vancomycin", "linezolid", "trimethoprim sulfamethoxazole",
        // "nitrofurantoin", "gentamicin", "amikacin", "rifampin", "isoniazid", "ethambutol", "pyrazinamide",

        // Completed: Antifungals / antivirals
        // "fluconazole", "itraconazole", "voriconazole", "terbinafine", "nystatin", "clotrimazole",
        // "acyclovir", "valacyclovir", "oseltamivir", "ribavirin",

        // Completed: Antiretrovirals
        // "abacavir", "lamivudine", "tenofovir", "emtricitabine", "efavirenz", "dolutegravir", "raltegravir",
        // "darunavir", "ritonavir",

        // Completed: Cardiovascular / antihypertensives
        // "amlodipine", "lisinopril", "enalapril", "ramipril", "losartan", "valsartan", "telmisartan",
        // "candesartan", "metoprolol", "atenolol", "carvedilol", "bisoprolol", "propranolol hydrochloride",
        // "hydrochlorothiazide", "chlorthalidone", "furosemide", "spironolactone", "amiloride",
        // "verapamil", "diltiazem", "nifedipine", "hydralazine", "methyldopa", "clonidine", "doxazosin",

        // Completed: Lipid lowering
        // "atorvastatin", "simvastatin", "rosuvastatin", "pravastatin", "lovastatin", "fluvastatin",
        // "ezetimibe", "fenofibrate", "gemfibrozil",

        // Completed: Antiarrhythmics / anticoagulants
        // "digoxin", "amiodarone", "warfarin", "heparin", "enoxaparin", "dabigatran", "rivaroxaban",
        // "apixaban", "edoxaban", "clopidogrel", "ticagrelor", "aspirin low dose",

        // Completed: Diabetes
        // "metformin", "glipizide", "glyburide", "glimepiride", "pioglitazone", "sitagliptin",
        // "empagliflozin", "dapagliflozin", "canagliflozin", "insulin glargine", "insulin lispro",
        // "insulin aspart", "insulin detemir", "liraglutide", "semaglutide", "dulaglutide",

        // Completed: Thyroid / endocrine
        // "levothyroxine", "methimazole", "propylthiouracil", "liothyronine", "hydrocortisone",
        // "prednisone", "prednisolone", "dexamethasone", "methylprednisolone", "fludrocortisone",
        // "testosterone", "estradiol", "medroxyprogesterone",

        // Completed: Respiratory
        // "albuterol", "salmeterol", "formoterol", "ipratropium", "tiotropium", "budesonide",
        // "fluticasone", "beclomethasone", "montelukast", "theophylline", "guaifenesin",
        // "dextromethorphan", "pseudoephedrine",

        // Partially completed: GI. Continue from bisacodyl to avoid repeating OpenFDA requests.
        // "omeprazole", "esomeprazole", "lansoprazole", "pantoprazole", "rabeprazole", "ranitidine",
        // "famotidine", "sucralfate", "misoprostol", "ondansetron", "metoclopramide", "promethazine",
        // "loperamide", "docusate sodium",
        "bisacodyl", "senna", "lactulose", "polyethylene glycol",
        "mesalamine", "sulfasalazine", "infliximab",

        // Psychiatry / CNS
        "sertraline", "fluoxetine", "paroxetine", "citalopram", "escitalopram", "venlafaxine",
        "duloxetine", "bupropion", "mirtazapine", "trazodone", "amitriptyline", "nortriptyline",
        "lithium carbonate", "valproic acid", "lamotrigine", "carbamazepine", "gabapentin",
        "pregabalin", "phenytoin", "levetiracetam", "topiramate", "olanzapine", "risperidone",
        "quetiapine", "aripiprazole", "haloperidol", "clozapine", "ziprasidone", "lorazepam",
        "diazepam", "alprazolam", "clonazepam", "midazolam", "zolpidem", "buspirone",

        // Parkinson's / neurology
        "levodopa carbidopa", "pramipexole", "ropinirole", "amantadine", "donepezil", "memantine",
        "rivastigmine", "riluzole", "baclofen", "tizanidine", "cyclobenzaprine",

        // Allergy / antihistamines
        "loratadine", "cetirizine", "fexofenadine", "diphenhydramine", "hydroxyzine", "montelukast sodium",
        "epinephrine",

        // Dermatology
        "hydrocortisone cream", "betamethasone", "clobetasol", "triamcinolone", "mupirocin",
        "benzoyl peroxide", "tretinoin", "adapalene", "isotretinoin", "permethrin", "salicylic acid",
        "clindamycin topical", "silver sulfadiazine",

        // Ophthalmology
        "latanoprost", "timolol", "brimonidine", "dorzolamide", "tobramycin ophthalmic",
        "cyclopentolate", "tropicamide", "atropine",

        // Vitamins / minerals / supplements
        "ascorbic acid", "cholecalciferol", "ergocalciferol", "folic acid", "cyanocobalamin",
        "thiamine", "riboflavin", "niacin", "pyridoxine", "ferrous sulfate", "calcium carbonate",
        "potassium chloride", "magnesium oxide", "zinc sulfate", "multivitamin",

        // Oncology / immunosuppressants
        "methotrexate", "tamoxifen", "anastrozole", "letrozole", "cyclophosphamide", "doxorubicin",
        "paclitaxel", "cisplatin", "carboplatin", "fluorouracil", "capecitabine", "imatinib",
        "rituximab", "azathioprine", "cyclosporine", "tacrolimus", "mycophenolate",

        // Gout / musculoskeletal / bone
        "allopurinol", "colchicine", "febuxostat", "alendronate", "risedronate", "zoledronic acid",
        "calcitriol",

        // Anesthesia / sedation / muscle relaxants
        "lidocaine", "bupivacaine", "propofol", "ketamine", "atropine sulfate", "succinylcholine",
        "rocuronium",

        // Fluids / electrolytes / misc
        "sodium chloride", "dextrose", "sodium bicarbonate", "activated charcoal", "chlorhexidine",

        // Contraceptives
        "ethinylestradiol levonorgestrel", "norethindrone", "medroxyprogesterone acetate",

        // Brand names are intentionally excluded: this seed endpoint searches openfda.generic_name,
        // so brand-name terms add requests without reliably improving generic-drug coverage.
    };

    public MedicamentSeedService(
        IOpenFdaClient client,
        IMedicamentRepository repository,
        ILogger<MedicamentSeedService> logger)
    {
        _client = client;
        _repository = repository;
        _logger = logger;
    }

    public async Task<MedicamentSeedResult> SeedAsync(int targetCount, CancellationToken cancellationToken = default)
    {
        var target = Math.Clamp(targetCount, 1, MAX_TARGET_COUNT);
        var result = new MedicamentSeedResult();

        var existingIds = await _repository.GetExistingOpenFdaIdsAsync(cancellationToken);
        var genericNameCounts = await _repository.GetGenericNameCountsAsync(cancellationToken);
        var buffer = new List<Drug>();

        foreach (var term in SearchTerms)
        {
            if (result.TotalInserted >= target)
            {
                result.ReachedTarget = true;
                break;
            }

            result.QueriesUsed++;

            for (var page = 0; page < MAX_PAGES_PER_QUERY; page++)
            {
                if (result.TotalInserted + buffer.Count >= target)
                {
                    break;
                }

                MedicamentPage pageResult;
                try
                {
                    pageResult = await _client.SearchMedicamentsAsync(term, PAGE_SIZE, page * PAGE_SIZE, cancellationToken);
                    result.RequestsMade++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "OpenFDA request failed for seed term '{Term}' (page {Page}), skipping", term, page);
                    break;
                }

                if (pageResult.Items.Count == 0)
                {
                    break;
                }

                foreach (var candidate in pageResult.Items)
                {
                    if (string.IsNullOrWhiteSpace(candidate.OpenFdaId) || !existingIds.Add(candidate.OpenFdaId))
                    {
                        result.TotalSkippedDuplicates++;
                        continue;
                    }

                    var genericNameKey = GetGenericNameKey(candidate);
                    if (genericNameKey != null)
                    {
                        genericNameCounts.TryGetValue(genericNameKey, out var existingCount);
                        if (existingCount >= MAX_VARIANTS_PER_GENERIC_NAME)
                        {
                            result.TotalSkippedByLimit++;
                            continue;
                        }

                        genericNameCounts[genericNameKey] = existingCount + 1;
                    }

                    buffer.Add(candidate);
                }

                if (buffer.Count >= SAVE_BATCH_SIZE)
                {
                    result.TotalInserted += await _repository.AddRangeAsync(buffer, cancellationToken);
                    buffer.Clear();
                }

                if (pageResult.Items.Count < PAGE_SIZE)
                {
                    // No more pages available for this term.
                    break;
                }

                await Task.Delay(DelayBetweenRequests, cancellationToken);
            }
        }

        if (buffer.Count > 0)
        {
            result.TotalInserted += await _repository.AddRangeAsync(buffer, cancellationToken);
        }

        result.ReachedTarget = result.TotalInserted >= target;

        _logger.LogInformation(
            "Medicament seeding finished. Inserted: {Inserted}, Skipped duplicates: {Skipped}, Skipped by generic limit: {SkippedByLimit}, Queries used: {Queries}, Requests made: {Requests}",
            result.TotalInserted, result.TotalSkippedDuplicates, result.TotalSkippedByLimit, result.QueriesUsed, result.RequestsMade);

        return result;
    }

    private static string? GetGenericNameKey(Drug drug)
    {
        if (string.IsNullOrWhiteSpace(drug.GenericName))
        {
            return null;
        }

        return string.Join(' ', drug.GenericName.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToUpperInvariant();
    }
}
