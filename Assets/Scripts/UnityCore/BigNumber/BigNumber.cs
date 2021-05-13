using System;
using UnityEngine;

namespace CoolEffect.CoinsFly
{
    /// <summary>
    /// Notice that:BigNumber is positive,so minus
    /// </summary>
    [Serializable]
    public class BigNumber
    {
        #region Constant

        private const int MAXUnitsDValue = 5;
        private const double BaseNumber = 0.001;
        private const int Thousand = 1000;
        private static readonly BigNumber Zero = new BigNumber();

        #endregion


        #region Fields

        /// <summary>
        ///  number part : range[1,1000)
        /// </summary>
        public double number;

        /// <summary>
        ///  fraction part
        /// </summary>
        public int units;

        #endregion

        #region Constructors

        public BigNumber()
        {
            number = 0;
            units = 0;
        }

        #endregion


        #region Setter

        public BigNumber SetNumber(double num)
        {
            if (num < double.Epsilon)
            {
                throw new Exception("Number part must be positive.");
            }

            number = num;
            units = 0;
            TransformUnits();
            return this;
        }

        public BigNumber SetNumber(double num, int unit)
        {
            if (num < double.Epsilon)
            {
                throw new Exception("Number part must be positive.");
            }

            number = num;
            units = unit;
            TransformUnits();
            return this;
        }

        public BigNumber SetNumber(BigNumber num)
        {
            number = num.number;
            units = num.units;
            TransformUnits();
            return this;
        }

        #endregion

        #region OperationOveride

        public static bool operator <(BigNumber prev, BigNumber next)
        {
            return !next.ExactEquals(prev) && next.IsGreaterThanOrEqualsTo(prev);
        }

        public static bool operator <=(BigNumber prev, BigNumber next)
        {
            return next.IsGreaterThanOrEqualsTo(prev);
        }

        public static bool operator >(BigNumber prev, BigNumber next)
        {
            return !prev.ExactEquals(next) && prev.IsGreaterThanOrEqualsTo(next);
        }

        public static bool operator >=(BigNumber prev, BigNumber next)
        {
            return prev.IsGreaterThanOrEqualsTo(next);
        }

        #endregion

        #region Comparison

        public bool ExactEquals(BigNumber comparedNumber)
        {
            return units == comparedNumber.units && Math.Abs(number - comparedNumber.number) < 0.0001;
        }

        public bool IsGreaterThanOrEqualsTo(BigNumber comparedNumber)
        {
            if (units == comparedNumber.units)
            {
                return number >= comparedNumber.number;
            }

            return units >= comparedNumber.units;
        }

        #endregion

        #region Operation

        /// <summary>
        /// plus
        /// </summary>
        /// <param name="deltaNumber"></param>
        /// <returns></returns>
        public BigNumber Add(BigNumber deltaNumber)
        {
            var unitsD_value = Math.Abs(units - deltaNumber.units);
            if (units > deltaNumber.units)
            {
                number += deltaNumber.number * Math.Pow(BaseNumber, unitsD_value);
            }
            else
            {
                var addRealNum = deltaNumber.number;
                addRealNum += number * Math.Pow(BaseNumber, unitsD_value);
                number = addRealNum;
                units = deltaNumber.units;
            }

            TransformUnits();
            return this;
        }

        /// <summary>
        /// minus
        /// </summary>
        /// <param name="deltaNumber"></param>
        /// <returns></returns>
        public BigNumber Minus(BigNumber deltaNumber)
        {
            if (deltaNumber.IsGreaterThanOrEqualsTo(this))
            {
                Debug.LogWarning(
                    "Notice that:BigNumber is positive,so dont minus which is greater!This operator may return 0.");
                number = 0;
                units = 0;
                return this;
            }

            Debug.Assert(units >= deltaNumber.units);
            var unitsDelta = units - deltaNumber.units;
            //Ignore when delta units is more then MAXUnitsDValue
            if (unitsDelta <= MAXUnitsDValue)
            {
                number -= deltaNumber.number * Math.Pow(BaseNumber, unitsDelta);
            }

            TransformUnits();
            return this;
        }

        /// <summary>
        /// divide normalization
        /// </summary>
        /// <param name="divide"></param>
        /// <returns></returns>
        public float Divide01(BigNumber divide)
        {
            if (divide <= Zero) return 1;

            if (units < divide.units)
            {
                if (divide.units - units == 1)
                {
                    return (float) (number / (divide.number * Thousand));
                }

                return 0;
            }

            if (units > divide.units)
            {
                if (units - divide.units == 1)
                {
                    return (float) Math.Min(1, (number * Thousand) / divide.number);
                }

                return 1;
            }

            return Mathf.Clamp01((float) (number / divide.number));
        }

        /// <summary>
        /// sqrt
        /// </summary>
        /// <returns></returns>
        public BigNumber Sqrt()
        {
            //10的次幂
            int real10power;
            if (units == 0)
            {
                number = Math.Sqrt(number);
            }
            else
            {
                real10power = units * 3;
                if (real10power % 2 == 1)
                {
                    real10power -= 1;
                    number = Math.Sqrt(number * 10);
                }
                else
                {
                    number = Math.Sqrt(number);
                }

                real10power /= 2;

                if (real10power < 3)
                {
                    number *= Math.Pow(10, real10power);
                    units = 0;
                }
                else
                {
                    real10power -= 3;
                    units = 1;
                    number *= Math.Pow(10, real10power % 3);
                    units += Mathf.FloorToInt(real10power / 3f);
                }
            }

            return this;
        }

        public BigNumber Times(double num)
        {
            if (num < double.Epsilon)
            {
                throw new Exception("Number part must be positive.");
            }

            number *= num;
            TransformUnits();
            return this;
        }

        public BigNumber Times(BigNumber num)
        {
            number *= num.number;
            units += num.units;
            TransformUnits();
            return this;
        }

        public BigNumber Divide(BigNumber num)
        {
            number /= num.number;
            units -= num.units;
            TransformUnits();
            return this;
        }

        #endregion


        #region Transform

        public double RevertToDouble()
        {
            return number * Math.Pow(Thousand, units);
        }

        /// <summary>
        /// transform units to make number part between [0,1000)
        /// </summary>
        public void TransformUnits()
        {
            while (number >= Thousand)
            {
                number /= Thousand;
                units += 1;
            }

            while (number < 1)
            {
                if (units > 0)
                {
                    units--;
                    number *= Thousand;
                }
                else
                {
                    break;
                }
            }
        }

        public void Reset()
        {
            number = 0;
            units = 0;
        }

        #endregion

        #region Other Methods

        public BigNumber Clone()
        {
            return new BigNumber().SetNumber(this);
        }

        public bool IsZero()
        {
            return number < double.Epsilon && units <= 0;
        }

        public static void BigNumberPow(double x, double y, ref BigNumber bigNumber)
        {
            bigNumber.SetNumber(1);
            while (y > 1000)
            {
                bigNumber.Times(Math.Pow(x, 1000));
                y -= 1000;
            }

            bigNumber.Times(Math.Pow(x, y));
        }

        #endregion
    }
}