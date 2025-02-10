using Microsoft.ML.Data;

namespace DrugDosageApp.Models
{
    public class DrugDosagePrediction
    {
        [ColumnName("PredictedLabel")]
        public string PredictedDosageForm { get; set; }

        public float[] Score { get; set; }
    }
}