using System;
using System.Reflection; //pour construire une instance de StatisticFormula 
using System.Windows.Forms.DataVisualization.Charting; //pour les methodes de la classe StatisticFormula

namespace Pricer
{
    public class EurUsdCall 
    {
        #region "Parametres de l'option "
        
        protected double maturity;
        protected double strike;
        protected static double volatility;

        #endregion

        #region "Methodes : Constructeur + BS Pricer + Calculateur de Vega"
        
        public EurUsdCall(double maturityParam, double strikeParam)
        {
            maturity = maturityParam;
            strike = strikeParam;
        }
        public double BlackScholesPrice(double domesticInterestRateParam, double foreignInterestRateParam,double exchangeRateSpotParam,double volatilityParam)
        {
            MarketQuotation.domesticInterestRate = domesticInterestRateParam;
            MarketQuotation.foreignInterestRate = foreignInterestRateParam;
            MarketQuotation.exchangeRateSpot = exchangeRateSpotParam;
            volatility = volatilityParam;
            double d1 = (Math.Log(MarketQuotation.exchangeRateSpot / strike) + (MarketQuotation.domesticInterestRate - MarketQuotation.foreignInterestRate + (1 / 2) * volatility * volatility) * maturity) / (volatility * Math.Sqrt(maturity));
            double d2 = d1 - volatility * Math.Sqrt(maturity);

            // StatisticFormula est sans constructeur, j ai utilise cette solution(ligne 33) pour pouvoir utiliser les methodes non static de la classe StatisticFormula
            var StatisticFormula =(StatisticFormula)typeof(StatisticFormula).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance,null, Type.EmptyTypes, null).Invoke(null);
            return Math.Exp(-MarketQuotation.foreignInterestRate * maturity) * MarketQuotation.exchangeRateSpot*StatisticFormula.NormalDistribution(d1) - strike * Math.Exp(-MarketQuotation.domesticInterestRate * maturity)*StatisticFormula.NormalDistribution(d2);
            //return Math.Exp(-MarketQuotation.foreignInterestRate * maturity) * MarketQuotation.exchangeRateSpot  - strike * Math.Exp(-MarketQuotation.domesticInterestRate * maturity) ;
        }
        public double Vega(double strikeParam)
        {
            double d1 = (Math.Log(MarketQuotation.exchangeRateSpot / strikeParam) + (MarketQuotation.domesticInterestRate - MarketQuotation.foreignInterestRate + (1 / 2) * volatility * volatility) * maturity) / (volatility * Math.Sqrt(maturity));
            return MarketQuotation.exchangeRateSpot * Math.Sqrt(maturity) * Math.Exp(-(d1 * d1) / 2) * (1 / Math.Sqrt(2 * Math.PI)) * Math.Exp(-MarketQuotation.foreignInterestRate * maturity);
           
        }
        #endregion
    }

    public class MarketQuotation : EurUsdCall
    {
        #region " Cotations du marche "
        public static double domesticInterestRate;
        public static double foreignInterestRate;
        public static double exchangeRateSpot;
        public double atmVolatility;
        public double vwbVolatility;
        public double rrVolatility;
        public double delta25CallVolatility;
        public double delta25PutVolatility;
        public double atmStrike;
        public double delta25PutStrike;
        public double delta25CallStrike;
        
        #endregion

        #region "Constructeur et calculateur des cotations de base a partir des donnees du marche "
        public MarketQuotation(double domesticInterestRateParam,double foreignInterestRateParam,double exchangeRateSpotParam, double atmVolatilityParam,double vwbVolatilityParam,double rrVolatilityParam,double maturityParam,double strikeParam):base(maturityParam,strikeParam)
        {
            domesticInterestRate = domesticInterestRateParam;
            foreignInterestRate = foreignInterestRateParam;
            exchangeRateSpot = exchangeRateSpotParam;
            atmVolatility = atmVolatilityParam;
            vwbVolatility = vwbVolatilityParam;
            rrVolatility = rrVolatilityParam;
            delta25CallVolatility = atmVolatility + vwbVolatility + (1 / 2) * rrVolatility;
            delta25PutVolatility = atmVolatility + vwbVolatility - (1 / 2) * rrVolatility;
            atmStrike = exchangeRateSpot * Math.Exp((domesticInterestRate - foreignInterestRate + (1 / 2) * atmVolatility * atmVolatility) * maturity);
            var StatisticFormula = (StatisticFormula)typeof(StatisticFormula).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null).Invoke(null);
            double alpha = -StatisticFormula.InverseNormalDistribution((1 / 4) * Math.Exp(foreignInterestRate * maturity));
            //double alpha = 1;
            delta25PutStrike = exchangeRateSpot * Math.Exp(-alpha * delta25PutVolatility * Math.Sqrt(maturity) + (domesticInterestRate - foreignInterestRate + (1 / 2) * delta25PutVolatility * delta25PutVolatility) * maturity);
            delta25CallStrike= exchangeRateSpot * Math.Exp(alpha * delta25CallVolatility * Math.Sqrt(maturity) + (domesticInterestRate - foreignInterestRate + (1 / 2) * delta25PutVolatility * delta25CallVolatility) * maturity);
        }
        #endregion

        #region " Calculateurs des poids du portefeuille de replication " 
        public double weight1()
        {
            return (this.Vega(strike) / this.Vega(delta25PutStrike)) * ((Math.Log(atmStrike/strike)*Math.Log(delta25CallStrike/strike)/(Math.Log(atmStrike / delta25PutStrike) * Math.Log(delta25CallStrike /delta25PutStrike))));
        }
        public double weight2()
        {
            return (this.Vega(strike) / this.Vega(atmStrike)) * ((Math.Log(strike /delta25PutStrike) * Math.Log(delta25CallStrike / strike) / (Math.Log(atmStrike / delta25PutStrike) * Math.Log(delta25CallStrike / atmStrike))));
        }
        public double weight3()
        {
            return (this.Vega(strike) / this.Vega(delta25CallStrike)) * ((Math.Log(strike /delta25PutStrike) * Math.Log(strike / atmStrike) / (Math.Log(delta25CallStrike / delta25PutStrike) * Math.Log(delta25CallStrike / atmStrike))));
        }
        #endregion
    }

    class Program
    {
        static void Main(string[] args)
        {
            MarketQuotation exemple = new MarketQuotation(0.002, 0.0, 1.2, 0.09, 0.08, 0.085, 3, 1.1);
            double blackScholesPrice = exemple.BlackScholesPrice(0.002, 0.0, 1.2, 0.08);
            double x1 = exemple.weight1();
            double x2 = exemple.weight2();
            double x3 = exemple.weight3();

            double marketPrice1 = 5;
            double marketPrice2 = 6;
            double marketPrice3 = 4;

            double price = blackScholesPrice + x1 * marketPrice1 + x2 * marketPrice2 + x3 * marketPrice3;

            Console.WriteLine("price= " + price);
        }
    }
}
