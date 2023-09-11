using System;
using System.Collections.Generic;
using System.Text;

namespace SharpFM.Core
{
    public class FileMakerField
    {
        public int FileMakerFieldId { get; set; }

        public string Name { get; set; } = null!;

        public string DataType { get; set; } = null!;

        public string FieldType { get; set; } = null!;

        public string? Comment { get; set; }

        public bool NotEmpty { get; set; }

        public bool Unique { get; set; }
    }
}
