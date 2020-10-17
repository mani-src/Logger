using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logger
{
    public static class Extensions
    {
        public static string Multiply(this string sourceString, int numberOfTimes)
        {
            try
            {
                StringBuilder sbTextRepeatBuilder = new StringBuilder();
                for (int i = 0; i < numberOfTimes; i++)
                {
                    sbTextRepeatBuilder.Append(sourceString);
                }
                return sbTextRepeatBuilder.ToString();
            }
            catch (Exception exp)
            {
                throw exp;
            }
        }
    }
}
