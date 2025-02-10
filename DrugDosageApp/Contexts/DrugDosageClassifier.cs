using System.Text.RegularExpressions;
using System.Threading.Tasks.Dataflow;
using DrugDosageApp.Models;
using DrugDosageApp.Services;
using Microsoft.ML;

namespace DrugDosageApp.Contexts
{
    public class DrugDosageClassifier
    {
        private readonly MLContext _mlContext;
        private ITransformer _trainedModel;
        private readonly string _modelPath = "drug_dosage_model.zip";
        private readonly NDC11Validator _ndcValidator;
        private readonly ExecutionDataflowBlockOptions _defaultOptions;

        public DrugDosageClassifier(NDC11Validator ndcValidator)
        {
            _mlContext = new MLContext(seed: 42);
            _ndcValidator = ndcValidator;
            _defaultOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                BoundedCapacity = 100
            };
        }

        public async Task TrainModelAsync(IEnumerable<DrugDosage> trainingData)
        {
            var pipeline = CreateTrainingPipeline();
            var trainingBlock = new TransformBlock<IEnumerable<DrugDosage>, ITransformer>(
                async data =>
                {
                    var trainingDataView = _mlContext.Data.LoadFromEnumerable(data);
                    return await Task.Run(() => pipeline.Fit(trainingDataView));
                }, _defaultOptions);

            var saveBlock = new ActionBlock<ITransformer>(
                async model =>
                {
                    _trainedModel = model;
                    await Task.Run(() => _mlContext.Model.Save(model, null, _modelPath));
                }, _defaultOptions);

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            trainingBlock.LinkTo(saveBlock, linkOptions);

            await trainingBlock.SendAsync(trainingData);
            trainingBlock.Complete();
            await saveBlock.Completion;
        }

        private IEstimator<ITransformer> CreateTrainingPipeline()
        {
            var pipeline = _mlContext.Transforms.Conversion.MapValueToKey(
                    inputColumnName: nameof(DrugDosage.DosageForm),
                    outputColumnName: "Label")
                .Append(_mlContext.Transforms.Text.FeaturizeText(
                    inputColumnName: nameof(DrugDosage.DrugName),
                    outputColumnName: "DrugNameFeatures"))
                .Append(_mlContext.Transforms.Text.FeaturizeText(
                    inputColumnName: nameof(DrugDosage.Strength),
                    outputColumnName: "StrengthFeatures"))
                .Append(_mlContext.Transforms.Text.FeaturizeText(
                    inputColumnName: nameof(DrugDosage.Route),
                    outputColumnName: "RouteFeatures"))
                .Append(_mlContext.Transforms.Concatenate("Features",
                    "DrugNameFeatures", "StrengthFeatures", "RouteFeatures"))
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy())
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            return pipeline;
        }

        public class DosageValidationResult
        {
            public bool IsValid { get; set; }
            public string PredictedDosageForm { get; set; }
            public float Confidence { get; set; }
            public List<string> Warnings { get; set; } = new();
            public List<string> Errors { get; set; } = new();
        }

        public async Task<DosageValidationResult> ValidateDosageAsync(DrugDosage dosage)
        {
            var validationBlock = new TransformBlock<DrugDosage, DosageValidationResult>(
                async input =>
                {
                    var result = new DosageValidationResult();

                    // Validate NDC-11
                    if (!_ndcValidator.IsValidNDC11(input.NDC11))
                    {
                        result.Errors.Add("Invalid NDC-11 code");
                        result.IsValid = false;
                        return result;
                    }

                    // Validate strength format
                    if (!ValidateStrength(input.Strength))
                    {
                        result.Warnings.Add("Unusual strength format");
                    }

                    // Predict dosage form if model is trained
                    if (_trainedModel != null)
                    {
                        var predictionEngine = _mlContext.Model.CreatePredictionEngine<DrugDosage, DrugDosagePrediction>(_trainedModel);
                        var prediction = await Task.Run(() => predictionEngine.Predict(input));

                        result.PredictedDosageForm = prediction.PredictedDosageForm;
                        result.Confidence = prediction.Score.Max();

                        if (prediction.PredictedDosageForm != input.DosageForm)
                        {
                            result.Warnings.Add($"Predicted dosage form ({prediction.PredictedDosageForm}) differs from input");
                        }
                    }

                    // Validate frequency
                    if (!ValidateFrequency(input.Frequency))
                    {
                        result.Warnings.Add("Unusual frequency pattern");
                    }

                    result.IsValid = result.Errors.Count == 0;
                    return result;
                }, _defaultOptions);

            validationBlock.Post(dosage);
            return await validationBlock.ReceiveAsync();
        }

        private static bool ValidateStrength(string strength)
        {
            // Basic strength pattern validation
            var strengthPattern = new Regex(@"^\d+(\.\d+)?\s*(mg|g|mcg|mL|%|unit|IU)$", RegexOptions.IgnoreCase);
            return strengthPattern.IsMatch(strength);
        }

        private static bool ValidateFrequency(string frequency)
        {
            // Common frequency patterns
            var frequencyPatterns = new[]
            {
            @"^\d+\s*times?\s*(daily|per\s*day)$",
            @"^every\s*\d+\s*(hours?|days?)$",
            @"^once\s*daily$",
            @"^twice\s*daily$",
            @"^three\s*times?\s*daily$",
            @"^four\s*times?\s*daily$",
            @"^as\s*needed$",
            @"^with\s*meals?$"
        };

            return frequencyPatterns.Any(pattern =>
                Regex.IsMatch(frequency, pattern, RegexOptions.IgnoreCase));
        }
    }
}