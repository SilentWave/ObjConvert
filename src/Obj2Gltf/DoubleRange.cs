using System;

namespace Arctron.Obj2Gltf
{
    /// <summary>
    /// a range expressed by Min and Max values
    /// </summary>
    public class DoubleRange
    {
        public DoubleRange()
        {
            Min = Double.MaxValue;
            Max = Double.MinValue;
        }
        public Double Min { get; set; }

        public Double Max { get; set; }

        public Boolean IsValid()
        {
            return Min <= Max;
        }

        public override String ToString()
        {
            return $"(Min: {Min}, Max: {Max})";
        }
    }
}
