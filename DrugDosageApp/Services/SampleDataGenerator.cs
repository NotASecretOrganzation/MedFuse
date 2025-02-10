// Sample training data generator

// Sample training data generator
using DrugDosageApp.Models;

namespace DrugDosageApp.Services
{
    public static class SampleDataGenerator
    {
        public static IEnumerable<DrugDosage> GenerateTrainingData()
        {
            return new List<DrugDosage>
        {
            new DrugDosage
            {
                NDC11 = "12345678901",
                DrugName = "Amoxicillin",
                Strength = "500 mg",
                DosageForm = "tablet",
                Route = "oral",
                Quantity = 30,
                Unit = "tablets",
                Frequency = "twice daily"
            },
            // Add more training examples
        };
        }
    }
}