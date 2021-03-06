﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Xml.Serialization.Configuration;
using Akka.Actor;
using ChartApp.Messages;

namespace ChartApp.Actors
{
    public class ChartingActor : ReceiveActor, IWithUnboundedStash
    {
        /// <summary>
        /// Max number of points in a series
        /// </summary>
        public const int MaxPoints = 250;
        /// <summary>
        /// Counter for position along x axis
        /// </summary>
        private int XPosCounter = 0;
        public IStash Stash { get; set; }
        
        #region Messages
        
        /// <summary>
        /// Toggles the pausing between charts
        /// </summary>
        public class TogglePause {}

        public class InitializeChart
        {
            public InitializeChart(Dictionary<string, Series> initialSeries)
            {
                InitialSeries = initialSeries;
            }

            public Dictionary<string, Series> InitialSeries { get; private set; }
        }

        public class AddSeries
        {
            public Series Series { get; }
            public AddSeries(Series series)
            {
                Series = series;
            }
        }

        public class RemoveSeries
        {
            public string SeriesName { get; }

            public RemoveSeries(string seriesName)
            {
                SeriesName = seriesName;
            }
        }

        #endregion

        private readonly Chart _chart;
        private readonly Button _pauseButton;
        private Dictionary<string, Series> _seriesIndex;

        public ChartingActor(Chart chart, Button pauseButton) : this(chart, new Dictionary<string, Series>(), pauseButton)
        {
        }

        public ChartingActor(Chart chart, Dictionary<string, Series> seriesIndex, Button pauseButton)
        {
            _chart = chart;
            _seriesIndex = seriesIndex;
            _pauseButton = pauseButton;
            
            Charting();
        }
        
        #region Behaviors

        private void Charting()
        {
            Receive<InitializeChart>(ic => HandleInitialize(ic));
            Receive<AddSeries>(addSeries => HandleAddSeries(addSeries));
            Receive<RemoveSeries>(removeSeries => HandleRemoveSeries(removeSeries));
            Receive<Metric>(metric => HandleMetrics(metric));
            Receive<TogglePause>(pause =>
            {
                SetPauseButtonText(true);
                BecomeStacked(Paused);
            });
        }

        private void Paused()
        {
            Receive<Metric>(metric => HandleMetricsPaused(metric));
            Receive<AddSeries>(addSeries => Stash.Stash());
            Receive<RemoveSeries>(removeSeries => Stash.Stash());
            Receive<TogglePause>(pause =>
            {
                SetPauseButtonText(false);
                UnbecomeStacked();
                Stash.UnstashAll();
            });
        }
        #endregion

        #region Individual Message Type Handlers

        private void HandleInitialize(InitializeChart ic)
        {
            if (ic.InitialSeries != null)
            {
                _seriesIndex = ic.InitialSeries;
            }
            
            _chart.Series.Clear();
            
            // set the axis up
            var area = _chart.ChartAreas[0];
            area.AxisX.IntervalType = DateTimeIntervalType.Number;
            area.AxisY.IntervalType = DateTimeIntervalType.Number;
            
            SetChartBoundaries();
            
            // attempt to render initial chart
            if (_seriesIndex.Any())
            {
                foreach (var series in _seriesIndex)
                {
                    // force the chart and internal index to use the same names
                    series.Value.Name = series.Key;
                    _chart.Series.Add(series.Value);
                }
            }
            
            SetChartBoundaries();
        }

        private void HandleAddSeries(AddSeries series)
        {
            if (!string.IsNullOrEmpty(series.Series.Name)
                && !_seriesIndex.ContainsKey(series.Series.Name))
            {
                _seriesIndex.Add(series.Series.Name, series.Series);
                _chart.Series.Add(series.Series);
                SetChartBoundaries();
            }
        }

        private void HandleRemoveSeries(RemoveSeries series)
        {
            if (!string.IsNullOrEmpty(series.SeriesName) && _seriesIndex.ContainsKey(series.SeriesName))
            {
                var seriesToRemove = _seriesIndex[series.SeriesName];
                _seriesIndex.Remove(series.SeriesName);
                _chart.Series.Remove(seriesToRemove);
                SetChartBoundaries();
            }
        }

        private void HandleMetrics(Metric metric)
        {
            if (!string.IsNullOrEmpty(metric.Series) && _seriesIndex.ContainsKey(metric.Series))
            {
                var series = _seriesIndex[metric.Series];
                series.Points.AddXY(XPosCounter++, metric.CounterValue);
                while(series.Points.Count > MaxPoints)
                    series.Points.RemoveAt(0);
                SetChartBoundaries();
            }
        }

        private void HandleMetricsPaused(Metric metric)
        {
            if (!string.IsNullOrEmpty(metric.Series) && _seriesIndex.ContainsKey(metric.Series))
            {
                var series = _seriesIndex[metric.Series];
                // set Y value to 0 when we're paused
                series.Points.AddXY(XPosCounter++, 0.0d);
                while (series.Points.Count > MaxPoints)
                    series.Points.RemoveAt(0);
                SetChartBoundaries();
            }
        }

        #endregion

        private void SetChartBoundaries()
        {
            double maxAxisX, maxAxisY, minAxisX, minAxisY = 0.0d;
            var allPoints = _seriesIndex.Values.SelectMany(x => x.Points).ToList();
            var yValues = allPoints.SelectMany(y => y.YValues).ToList();
            maxAxisX = XPosCounter;
            minAxisX = maxAxisX - MaxPoints;
            maxAxisY = yValues.Count > 0
                ? Math.Ceiling(yValues.Max())
                : 1.0d;
            minAxisY = yValues.Count > 0
                ? Math.Floor(yValues.Min())
                : 0.0d;
            if (allPoints.Count > 2)
            {
                var area = _chart.ChartAreas[0];
                area.AxisX.Minimum = minAxisX;
                area.AxisX.Maximum = maxAxisX;
                area.AxisY.Minimum = minAxisY;
                area.AxisY.Maximum = maxAxisY;
            }
        }

        private void SetPauseButtonText(bool paused)
        {
            _pauseButton.Text = $"{(!paused ? "PAUSE ||" : "RESUME ->")}";
        }
    }
}
