using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace DrugDosageApp.Models
{
    public class DrugDosage
    {
        [LoadColumn(0)]
        public string NDC11 { get; set; }

        [LoadColumn(1)]
        public string DrugName { get; set; }

        [LoadColumn(2)]
        public string Strength { get; set; }

        [LoadColumn(3)]
        public string DosageForm { get; set; }

        [LoadColumn(4)]
        public string Route { get; set; }

        [LoadColumn(5)]
        public float Quantity { get; set; }

        [LoadColumn(6)]
        public string Unit { get; set; }

        [LoadColumn(7)]
        public string Frequency { get; set; }
    }
}