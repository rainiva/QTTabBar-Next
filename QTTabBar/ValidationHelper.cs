using System;

namespace QTTabBarLib {
    internal static class ValidationHelper {
        public static int ValidateMinMax(int value, int min, int max) {
            int a = Math.Min(min, max);
            int b = Math.Max(min, max);
            if(value < a) {
                value = a;
            }
            else if(value > b) {
                value = b;
            }
            return value;
        }

        public static void ValidateMinMax(ref int value, int min, int max) {
            value = ValidateMinMax(value, min, max);
        }
    }
}
