﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoARIMA
{

    public enum DecayPartern
    {
        SLOW_DECAY,
        EXPONENTIAL_DECAY,
        ABRUPT_DECAY
    }

    public static class Configuration
    {
        public static int MAX_ARMA_ORDER = 25;
    }

    class Program
    {

        static void Main(string[] args)
        {
            string fileName = @"E:\PROJECT\FINAL PROJECT\Other\Test\airpass.dat";
            List<double> series = new List<double>();
            List<double> errorsSeason = new List<double>();
            List<double> errors = new List<double>();
            System.IO.StreamReader file = null;
            string line = null;
            try
            {
                file = new System.IO.StreamReader(fileName);
                while ((line = file.ReadLine()) != null)
                {
                    series.Add(Double.Parse(line));
                    errorsSeason.Add(Double.Parse(line));
                    errors.Add(0);
                }
            }
            catch (System.OutOfMemoryException outOfMemory)
            {
                series = null;
            }

            List<double> listAutocorrelation = new List<double>();
            List<double> listPartialAutocorrelation = new List<double>();
            List<double> listConfidenceLimit = new List<double>();
            List<double> listArimaCoeff = new List<double>();
            List<double> listSeasonArimaCoeff = new List<double>();
            int regularDifferencingLevel = 0;
            int seasonPartern = 0;
            int seasonDifferencingLevel = 0;
            int startIndex = 0;
            int dataSize = series.Count;
            int p,q,P,Q;


            ComputeAutocorrelation(series, startIndex, listAutocorrelation);
            ComputeConfidenceLimit(listAutocorrelation, dataSize, listConfidenceLimit);
            ComputePartialAutocorrelation(listAutocorrelation, listPartialAutocorrelation);
            double confidenceLimit = 1.96 / Math.Sqrt(dataSize);

            DrawSeriesData(series, dataSize);
            DrawAutocorrelation(listAutocorrelation, listConfidenceLimit);
            DrawPartialAutocorrelation(listPartialAutocorrelation, confidenceLimit);
            //RemoveNonstationarity(series, ref dataSize, out regularDifferencingLevel);
            //RemoveSeasonality(series, ref dataSize, out seasonPartern, out seasonDifferencingLevel);
            //ComputeArima(series, dataSize, seasonPartern, out p, out q, out P, out Q);

            //ComputeARMA(series, errors, p, q, listArimaCoeff);
            //ComputeARMA(series, errorsSeason, P, Q, listSeasonArimaCoeff);

            //List<double> testSeries = new List<double> { 1.0, 2.3, 3.5, 4.4, 5.4, 15.2, 16.4, 17.5, 18.5, 19.8, 30.0, 31.4, 32.6, 33.5, 34.8, 45.2 };
            int x = 0;
        }

        //Tested : OK
        public static void ComputeDifference(List<double> series, ref int startIndex, int d, int D, int s)
        {
            for (int i = 0; i < d; i++)
            {
                startIndex += 1;
                for (int j = series.Count-1; j >= startIndex; j--)
                {
                    series[j] = series[j] - series[j-1];
                }
            }

            for (int i = 0; i < D; i++)
            {
                startIndex += s;
                for (int j = series.Count - 1; j >= startIndex; j--)
                {
                    series[j] = series[j] - series[j-s];
                }
            }
        }

        //Tested : OK
        public static void RevertDifference(List<double> series, ref int startIndex, int d, int D, int s)
        {
            for (int i = 0; i < D; i++)
            {
                for (int j = startIndex; j < series.Count; j++)
                {
                    series[j] = series[j] + series[j-s];
                }
                startIndex -= s;
            }

            //for (int i = 0; i < d; i++)
            {
                for (int j = startIndex; j < series.Count; j++)
                {
                    series[j] = series[j] + series[j-1];
                }
                startIndex -= 1;
            }
        }

        public static void EstimateRegularARMA(List<double> series, List<double> errors, int startIndex, int pCoef, int qCoef, List<double> listArimaCoeff)
        {
            EstimateSeasonARMA(series, errors, startIndex, 1, pCoef, qCoef, listArimaCoeff);
        }

        public static void EstimateSeasonARMA(List<double> series, List<double> errors, int startIndex, int season, int pCoef, int qCoef, List<double> listArimaCoeff)
        {
            Matrix observationVector = new Matrix(1 + pCoef + qCoef, 1);
            Matrix parameterVector = new Matrix(1 + pCoef + qCoef, 1);
            Matrix gainFactor = new Matrix(1 + pCoef + qCoef, 1);
            Matrix invertedCovarianceMatrix = new Matrix(1 + pCoef + qCoef, 1 + pCoef + qCoef);
            double prioriPredictionError;
            double posterioriPredictionError;

            //Phase 1 - Set Initial Conditions
            //the observation vector
            observationVector[0, 0] = 1;
            for (int i = 1; i < pCoef + 1; i++)
            {
                observationVector[i, 0] = series[(i - 1)*season];
            }
            for (int i = 1; i < qCoef + 1; i++)
            {
                observationVector[pCoef + i, 0] = 0;
            }
            for (int i = 0; i < pCoef + qCoef + 1; i++)
            {
                invertedCovarianceMatrix[i, i] = Math.Pow(10, 6);
            }


            for (int i = pCoef * season; i < series.Count; i++)
            {
                //Phase 1
                observationVector[0, 0] = 1;
                for (int j = 1; j < pCoef + 1; j++)
                {
                    observationVector[j, 0] = series[i - j*season];
                }
                for (int j = 1; j < qCoef + 1; j++)
                {
                    if (i - j*season >= 0)
                    {
                        observationVector[pCoef + j, 0] = errors[i - j * season];
                    }
                    else
                    {
                        observationVector[pCoef + j, 0] = 0;
                    }
                }

                //Phase 2 - Estimate Parameters
                prioriPredictionError = series[i] - (Matrix.Transpose(observationVector) * parameterVector)[0, 0];
                double temp = 1 + (Matrix.Transpose(observationVector) * invertedCovarianceMatrix * observationVector)[0, 0];
                gainFactor = (invertedCovarianceMatrix * observationVector) / temp;
                parameterVector += gainFactor * prioriPredictionError;

                //Phase 3 - Prepare for Next Estimation 
                posterioriPredictionError = series[i] - (Matrix.Transpose(observationVector) * parameterVector)[0, 0];
                invertedCovarianceMatrix = invertedCovarianceMatrix - gainFactor * Matrix.Transpose(observationVector) * invertedCovarianceMatrix;
                errors[i] = posterioriPredictionError;
            }

            for (int i = 0; i < 1 + pCoef + qCoef; i++)
            {
                listArimaCoeff.Add(parameterVector[i, 0]);
            }
        }

        //Tested : OK
        public static void DrawPartialAutocorrelation(List<double> listAutocorrelation, double confidenceLimit)
        {
            List<double> listConfidenceLimit = new List<double>();
            for(int i=0; i<listAutocorrelation.Count; i++)
            {
                listConfidenceLimit.Add(confidenceLimit);
            }
            DrawAutocorrelation(listAutocorrelation, listConfidenceLimit, true);
        }

        //Tested : OK
        public static void DrawAutocorrelation(List<double> listAutocorrelation, List<double> listConfidenceLimit, bool isPACF = false)
        {
            Plot_Form form = new Plot_Form();
            if (!isPACF)
            {
                form.chart1.Titles["Title1"].Text = "Autocorrelation Function";
                form.chart1.ChartAreas["ChartArea1"].Axes[0].Title = "Lag";
                form.chart1.ChartAreas["ChartArea1"].Axes[1].Title = "ACF";
                form.chart1.Series[0].Name = "ACF";
            }
            else
            {
                form.chart1.Titles["Title1"].Text = "Partial Autocorrelation Function";
                form.chart1.ChartAreas["ChartArea1"].Axes[0].Title = "Lag";
                form.chart1.ChartAreas["ChartArea1"].Axes[1].Title = "PACF";
                form.chart1.Series[0].Name = "PACF";
            }

            int numData = listAutocorrelation.Count;
            form.chart1.ChartAreas[0].AxisX.Interval = Math.Ceiling(1.0 * numData / 20);

            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            series1.ChartArea = "ChartArea1";
            series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series1.Color = System.Drawing.Color.Red;
            series1.IsVisibleInLegend = false;

            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            series2.ChartArea = "ChartArea1";
            series2.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series2.Color = System.Drawing.Color.Red;
            series2.IsVisibleInLegend = false;


            for (int i = 0; i < listAutocorrelation.Count; i++)
            {
                System.Windows.Forms.DataVisualization.Charting.Series series = new System.Windows.Forms.DataVisualization.Charting.Series();
                series.ChartArea = "ChartArea1";
                series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
                series.Color = System.Drawing.Color.Blue;
                series.Points.AddXY(i, 0.0);
                series.Points.AddXY(i, listAutocorrelation[i]);
                series.IsVisibleInLegend = false;
                form.chart1.Series.Add(series);

                series1.Points.AddXY(i, listConfidenceLimit[i]);
                series2.Points.AddXY(i, -listConfidenceLimit[i]);
            }

            form.chart1.Series.Add(series1);
            form.chart1.Series.Add(series2);

            form.ShowDialog();
        }

        //Tested : OK
        public static void DrawSeriesData(List<double> series, int dataSize)
        {
            Plot_Form form = new Plot_Form();
            form.chart1.ChartAreas[0].AxisX.Interval = Math.Ceiling(1.0 * dataSize / 20);
            form.chart1.Titles["Title1"].Text = "Time series";
            form.chart1.Series["Data"].Color = System.Drawing.Color.Blue;
            for (int i = 0; i < dataSize; i++)
            {
                form.chart1.Series["Data"].Points.AddXY(i + 1, series.ElementAt(i));
            }
            form.ShowDialog();
        }

        //Tested : OK
        public static void ComputeAutocorrelation(List<double> series, int startIndex, List<double> listAutocorrelation)
        {
            int numAutocorrelation = ((series.Count - startIndex) / 4 > 50 ? 50 : (series.Count - startIndex) / 4);

            double mean = 0;
            for (int i = startIndex; i < series.Count; i++)
            {
                mean += series[i];
            }
            mean /= series.Count;

            double variance = 0;
            for (int i = startIndex; i < series.Count; i++)
            {
                variance += Math.Pow(series[i] - mean, 2);
            }
            variance /= series.Count;

            listAutocorrelation.Add(1);
            for (int lag = 1; lag < numAutocorrelation; lag++)
            {
                double temp = 0;
                for (int i = startIndex; i < series.Count - lag; i++)
                {
                    temp += (series[i] - mean) * (series[i + lag] - mean);
                }
                temp /= series.Count * variance;
                listAutocorrelation.Add(temp);
            }
        }

        //Tested : OK
        public static void ComputeConfidenceLimit(List<double> listAutocorrelation, int dataSize, List<double> listConfidenceLimit)
        {
            for (int i = 0; i < listAutocorrelation.Count; i++)
            {
                double temp = 0;
                for (int j = 0; j < i; j++)
                {
                    temp += Math.Pow(listAutocorrelation[j], 2);
                }
                temp = Math.Sqrt((1 + 2 * temp) / dataSize);
                listConfidenceLimit.Add(temp);
            }
        }

        //Tested : OK
        public static double GetPartialCorrelationAt(List<double> listPartialCorrelation, int i, int j)
        {
            int index = 0;
            if (i > 1)
            {
                index = (int)(i * (i - 1) / 2 + i - j);
            }
            return listPartialCorrelation[index];
        }

        //Tested : OK
        public static void SetPartialCorrelationAt(List<double> listPartialCorrelation, int i, int j, double value)
        {
            int index = 0;
            if (i > 1)
            {
                index = (int)(i * (i - 1) / 2 + i - j);
            }
            listPartialCorrelation[index] = value;
        }

        //Tested : OK
        public static void ComputePartialAutocorrelation(List<double> listAutocorrelation, List<double> listPartialAutocorrelation)
        {
            int lag = listAutocorrelation.Count;
            int numPartialCorrelation = (int)(lag * (lag + 1) / 2);
            for (int i = 0; i < numPartialCorrelation; i++)
            {
                listPartialAutocorrelation.Add(0);
            }
            for (int i = 1; i <= lag; i++)
            {
                double numerator = 0;
                double denominator = 0;
                double temp = 0;
                for (int j = 1; j <= i - 1; j++)
                {
                    temp += GetPartialCorrelationAt(listPartialAutocorrelation, i - 1, j) * listAutocorrelation[i - j -1];
                }
                numerator = listAutocorrelation[i-1] - temp;
                temp = 0;
                for (int j = 1; j <= i - 1; j++)
                {
                    temp += GetPartialCorrelationAt(listPartialAutocorrelation, i - 1, j) * listAutocorrelation[j - 1];
                }
                denominator = 1 - temp;
                temp = numerator / denominator;
                SetPartialCorrelationAt(listPartialAutocorrelation, i, i, temp);

                for (int j = 1; j < i; j++)
                {
                    temp = GetPartialCorrelationAt(listPartialAutocorrelation, i - 1, j) - GetPartialCorrelationAt(listPartialAutocorrelation, i, i) * GetPartialCorrelationAt(listPartialAutocorrelation, i - 1, i - j);
                    SetPartialCorrelationAt(listPartialAutocorrelation, i, j, temp);
                }
            }

            List<double> result = new List<double>();
            for (int i = 1; i <= lag; i++)
            {
                result.Add(GetPartialCorrelationAt(listPartialAutocorrelation, i, i));
            }
            listPartialAutocorrelation.Clear();
            for (int i = 0; i < result.Count; i++)
            {
                listPartialAutocorrelation.Add(result[i]);
            }
        }

        public static void ComputeSeason(List<double> listAutocorrelation, List<double> listConfidenceLimit, out int seasonPartern)
        {
            List<int> listHightFreLocation = new List<int>();
            List<int> listHightPosFreLocation = new List<int>();
            List<int> listHightNegFreLocation = new List<int>();
            List<int> levelOneLocation = new List<int>();
            seasonPartern = 0;

            for (int i = 0; i < listAutocorrelation.Count; i++)
            {
                if (listAutocorrelation[i] > listConfidenceLimit[i]*1.2)
                {
                    listHightFreLocation.Add(i);
                    listHightPosFreLocation.Add(i);
                }
                else if (listAutocorrelation[i] < -listConfidenceLimit[i]*1.2)
                {
                    listHightFreLocation.Add(i);
                    listHightNegFreLocation.Add(i);
                }
            }

            List<int> listSignificantDistance = new List<int>();
            for (int i = 0; i < listHightPosFreLocation.Count - 1; i++)
            {
                if (listHightPosFreLocation[i + 1] - listHightPosFreLocation[i] != 1)
                {
                    listSignificantDistance.Add(listHightPosFreLocation[i + 1] - listHightPosFreLocation[i]);
                }
            }
            for (int i = 0; i < listHightNegFreLocation.Count - 1; i++)
            {
                if (listHightNegFreLocation[i + 1] - listHightNegFreLocation[i] != 1)
                {
                    listSignificantDistance.Add(listHightNegFreLocation[i + 1] - listHightNegFreLocation[i]);
                }
            }

            List<int> listDistinctSignificantLocation = listSignificantDistance.Distinct().ToList();
            if (listDistinctSignificantLocation.Count == 0)
            {
                return;
            }
            int hightFrequencyDistance = listDistinctSignificantLocation.ElementAt(0);
            int frequency = listSignificantDistance.Count(item => item == hightFrequencyDistance);
            for (int i = 1; i < listDistinctSignificantLocation.Count; i++)
            {
                int newFrequency = listSignificantDistance.Count(item => item == listDistinctSignificantLocation[i]);
                if ((newFrequency > frequency) || (newFrequency == frequency && listDistinctSignificantLocation[i] > hightFrequencyDistance))
                {
                    hightFrequencyDistance = listDistinctSignificantLocation[i];
                    frequency = newFrequency;
                }
            }

            if (1.0 * frequency / listDistinctSignificantLocation.Count > 0.5)
            {
                seasonPartern = hightFrequencyDistance;
                return;
            }

            #region continue
            //tempList.Clear();
            //for (int i = 0; i < listAutocorrelationLocation.Count - 1; i++)
            //{
            //    levelLocation.Add(listAutocorrelationLocation[i + 1] - listAutocorrelationLocation[i]);
            //    if (listAutocorrelationLocation[i + 1] - listAutocorrelationLocation[i] != 1)
            //    {
            //        tempList.Add(listAutocorrelationLocation[i + 1] - listAutocorrelationLocation[i]);
            //    }
            //}

            //tempList1 = tempList.Distinct().ToList();
            //if (tempList1.Count == 0)
            //{
            //    return;
            //}
            //hightFrequencyValue = tempList1.ElementAt(0);
            //frequency = tempList.Count(item => item == hightFrequencyValue);
            //for (int i = 1; i < tempList1.Count; i++)
            //{
            //    int newFrequency = tempList.Count(item => item == tempList1[i]);
            //    if (newFrequency > frequency)
            //    {
            //        hightFrequencyValue = i;
            //        frequency = newFrequency;
            //    }
            //}

            //if (1.0 * frequency / tempList1.Count > 0.5)
            //{
            //    season = hightFrequencyValue;
            //    return;
            //}
            #endregion

        }

        public static DecayPartern ComputeDecayPartern(List<double> listAutocorrelation, double confidenceLimit)
        {
            DecayPartern decayPartern;
            List<double> listHighAutocorrelation = new List<double>();
            listHighAutocorrelation = listAutocorrelation.FindAll(item => Math.Abs(item) > confidenceLimit).ToList();
            double averageRateExchange = 0;
            if (listHighAutocorrelation.Count <= 1)
            {
                decayPartern = DecayPartern.ABRUPT_DECAY;
                return decayPartern;
            }

            for (int i = 0; i < listHighAutocorrelation.Count-1; i++)
            {
                averageRateExchange += Math.Abs(Math.Abs(listHighAutocorrelation[i]) - Math.Abs(listHighAutocorrelation[i + 1])) / Math.Abs(listHighAutocorrelation[i]);
            }
            averageRateExchange /= (listHighAutocorrelation.Count -1);

            if (averageRateExchange > 0.65)
            {
                decayPartern = DecayPartern.ABRUPT_DECAY;
            }
            else if (averageRateExchange < 0.1)
            {
                decayPartern = DecayPartern.SLOW_DECAY;
            }
            else
            {
                decayPartern = DecayPartern.EXPONENTIAL_DECAY;
            }
            return decayPartern;
        }

        public static DecayPartern ComputeDecayPartern(List<double> listAutocorrelation, List<double> listConfidenceLimit)
        {
            DecayPartern decayPartern;
            List<double> listHighAutocorrelation = new List<double>();
            for (int i = 0; i < listAutocorrelation.Count; i++)
            {
                if (Math.Abs(listAutocorrelation[i]) > Math.Abs(listConfidenceLimit[i]))
                {
                    listHighAutocorrelation.Add(listAutocorrelation[i]);
                }
            }
            if (listHighAutocorrelation.Count <= 1)
            {
                decayPartern = DecayPartern.ABRUPT_DECAY;
                return decayPartern;
            }

            double averageRateExchange = 0;
            for (int i = 0; i < listHighAutocorrelation.Count - 1; i++)
            {
                averageRateExchange += Math.Abs(Math.Abs(listHighAutocorrelation[i]) - Math.Abs(listHighAutocorrelation[i + 1])) / Math.Abs(listHighAutocorrelation[i]);
            }
            averageRateExchange /= (listHighAutocorrelation.Count -1);

            if (averageRateExchange > 0.65)
            {
                decayPartern = DecayPartern.ABRUPT_DECAY;
            }
            else if (averageRateExchange < 0.1)
            {
                decayPartern = DecayPartern.SLOW_DECAY;
            }
            else
            {
                decayPartern = DecayPartern.EXPONENTIAL_DECAY;
            }
            return decayPartern;
        }

        public static void RemoveNonstationarity(List<double> series, ref int startIndex, out int regularDifferencingLevel)
        {
            int dataSize = series.Count - startIndex;
            List<double> listAutocorrelation = new List<double>();
            List<double> listConfidenceLimit = new List<double>();

            regularDifferencingLevel = 0;
            while (true)
            {
                listAutocorrelation.Clear();
                listConfidenceLimit.Clear();
                dataSize = series.Count - startIndex;
                ComputeAutocorrelation(series, startIndex, listAutocorrelation);
                ComputeConfidenceLimit(listAutocorrelation, dataSize, listConfidenceLimit);
                DecayPartern decayPartern = ComputeDecayPartern(listAutocorrelation, listConfidenceLimit);
                if (decayPartern != DecayPartern.SLOW_DECAY)
                {
                    break;
                }

                startIndex += 1;
                regularDifferencingLevel++;
                for (int j = series.Count - 1; j >= startIndex; j--)
                {
                    series[j] = series[j] - series[j - 1];
                }
            }
        }

        //public static void RemoveSeasonality(List<double> series, ref int dataSize, out int seasonPartern, out int seasonDifferencingLevel)
        //{
        //    seasonPartern = 0;
        //    seasonDifferencingLevel = 0;
        //    List<double> listAutocorrelation = new List<double>();
        //    List<double> listConfidenceLimit = new List<double>();
        //    List<int> listAutocorrelationLocation = new List<int>();
        //    List<int> levelLocation = new List<int>();

        //    int endIndex = dataSize;

        //    while (true)
        //    {
        //        listAutocorrelation.Clear();
        //        listConfidenceLimit.Clear();
        //        ComputeAutocorrelation(series, listAutocorrelation);
        //        ComputeConfidenceLimit(listAutocorrelation, endIndex, listConfidenceLimit);
        //        int newSeasonPartern;
        //        ComputeSeason(listAutocorrelation, listConfidenceLimit, out newSeasonPartern);
        //        if (newSeasonPartern == 0)
        //        {
        //            break;
        //        }
        //        seasonPartern = newSeasonPartern;
        //        seasonDifferencingLevel++;
        //        endIndex -= seasonPartern;
        //        for (int j = 0; j < endIndex; j++)
        //        {
        //            series[j] = series[j + seasonPartern] - series[j];
        //        }
        //    }

        //    dataSize -= seasonPartern * seasonDifferencingLevel;
        //}

        public static void GetLastSignificant(List<double> listAutocorrelation, List<double> listConfidenceLimit, out int lag)
        {
            lag = 0;
            for (int i = 1; i < listAutocorrelation.Count; i++)
            {
                if (listAutocorrelation[i] > listConfidenceLimit[i] || listAutocorrelation[i] <- listConfidenceLimit[i])
                {
                    lag = i + 1;
                }
            }
        }

        public static void GetLastSignificant(List<double> listAutocorrelation, double confidenceLimit, out int lag)
        {
            lag = 0;
            for (int i = 1; i < listAutocorrelation.Count; i++)
            {
                if (listAutocorrelation[i] > confidenceLimit || listAutocorrelation[i] < -confidenceLimit)
                {
                    lag = i + 1;
                }
            }
        }

        //public static void ComputeArima(List<double> series, int dataSize, int seasonPartern, out int p, out int q, out int P, out int Q)
        //{
        //    p = q = P = Q = 0;

        //    List<double> listConfidenceLimit = new List<double>();
        //    List<double> listAutocorrelation = new List<double>();
        //    List<double> listPartialAutocorrelation = new List<double>();

        //    List<double> listSeasonConfidenceLimit = new List<double>();
        //    List<double> listSeasonAutocorrelation = new List<double>();
        //    List<double> listSeasonPartialCorrelation = new List<double>();

        //    List<double> listRegularConfidenceLimit = new List<double>();
        //    List<double> listRegularAutocorrelation = new List<double>();
        //    List<double> listRegularPartialCorrelation = new List<double>();

        //    ComputeAutocorrelation(series, listAutocorrelation);
        //    ComputePartialAutocorrelation(listAutocorrelation, listPartialAutocorrelation);
        //    ComputeConfidenceLimit(listAutocorrelation, dataSize, listConfidenceLimit);
        //    double confidenceLimit = 1.96 / Math.Sqrt(dataSize);

        //    for (int i = 0; i < seasonPartern; i++)
        //    {
        //        listRegularAutocorrelation.Add(listAutocorrelation[i]);
        //        listRegularConfidenceLimit.Add(listConfidenceLimit[i]);
        //        listRegularPartialCorrelation.Add(listPartialAutocorrelation[i]);
        //    }

        //    for (int i = 0; i < Math.Floor(1.0 * listAutocorrelation.Count / seasonPartern); i++)
        //    {
        //        listSeasonAutocorrelation.Add(listAutocorrelation[i * seasonPartern]);
        //        listSeasonConfidenceLimit.Add(listConfidenceLimit[i * seasonPartern]);
        //        listSeasonPartialCorrelation.Add(listPartialAutocorrelation[i * seasonPartern]);
        //    }

        //    //DrawAutocorrelation(listRegularAutocorrelation, listRegularConfidenceLimit);
        //    //DrawAutocorrelation(listRegularPartialCorrelation, confidenceLimit);
        //    //DrawAutocorrelation(listSeasonAutocorrelation, listSeasonConfidenceLimit);
        //    //DrawAutocorrelation(listSeasonPartialCorrelation, confidenceLimit);

        //    DecayPartern decayACF = ComputeDecayPartern(listRegularAutocorrelation, listRegularConfidenceLimit);
        //    DecayPartern decayPACF = ComputeDecayPartern(listRegularPartialCorrelation, confidenceLimit);
        //    DecayPartern decaySeasonACF = ComputeDecayPartern(listSeasonAutocorrelation, listSeasonConfidenceLimit);
        //    DecayPartern decaySeasonPACF = ComputeDecayPartern(listSeasonPartialCorrelation, confidenceLimit);

        //    if (decayACF == DecayPartern.ABRUPT_DECAY)
        //    {
        //        GetLastSignificant(listRegularAutocorrelation, listRegularConfidenceLimit, out p);
        //    }
        //    if (decayPACF == DecayPartern.ABRUPT_DECAY)
        //    {
        //        GetLastSignificant(listRegularPartialCorrelation, confidenceLimit, out q);
        //    }
        //    if (decayACF != DecayPartern.ABRUPT_DECAY && decayPACF != DecayPartern.ABRUPT_DECAY && p * q != 0)
        //    {
        //        p = q = 1;
        //    }
        //    if (decaySeasonACF == DecayPartern.ABRUPT_DECAY)
        //    {
        //        GetLastSignificant(listSeasonAutocorrelation, listSeasonConfidenceLimit, out P);
        //    }
        //    if (decaySeasonPACF == DecayPartern.ABRUPT_DECAY)
        //    {
        //        GetLastSignificant(listSeasonPartialCorrelation, confidenceLimit, out Q);
        //    }
        //    if (decaySeasonACF != DecayPartern.ABRUPT_DECAY && decaySeasonPACF != DecayPartern.ABRUPT_DECAY && P * Q != 0)
        //    {
        //        P = Q = 1;
        //    }
        //    int test = 0;
        //}
    
    }
}