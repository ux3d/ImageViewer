﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TextureViewer.Annotations;
using TextureViewer.Commands;

namespace TextureViewer.Models.Filter
{
    public class FiltersModel
    {
        private List<FilterModel> filter = new List<FilterModel>();
        public IReadOnlyList<FilterModel> Filter => filter;
        public int NumFilter => filter.Count;

        public int StatisticsPoint { get; private set; } = 0;

        #region Change Event
        public class FiltersChangedEventArgs : EventArgs
        {
            public bool[] Changed { get; }

            public FiltersChangedEventArgs(bool[] changed)
            {
                Changed = changed;
            }
        }

        public delegate void FiltersChangedEventHandler(object sender, FiltersChangedEventArgs args);
        /// <summary>
        /// this will be triggered if the model changes due to:
        /// - Apply()
        /// </summary>
        public event FiltersChangedEventHandler Changed;
        #endregion

        /// <summary>
        /// returns if the shader is in use by the active model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool IsUsed(FilterModel model)
        {
            return filter.Any(f => ReferenceEquals(f, model));
        }

        /// <summary>
        /// Replaces the old filters with the new list of filters.
        /// </summary>
        /// <param name="models">new filter configuration</param>
        /// <param name="statisticsPoint">index of the statistics point</param>
        /// <param name="context"></param>
        /// <param name="changed">This indicates which image equations were changed by the new filter configuration</param>
        public void Apply(List<FilterModel> models, int statisticsPoint, OpenGlContext context, bool[] changed)
        {
            DisposeUnusedFilter(models, filter, context);
            filter = models;
            Debug.Assert(statisticsPoint >= 0 && statisticsPoint <= filter.Count);
            StatisticsPoint = statisticsPoint;
            OnChanged(changed);
        }

        /// <summary>
        /// disposes all filters from old list which are no longer used in new list
        /// </summary>
        /// <param name="newList"></param>
        /// <param name="oldList"></param>
        /// <param name="context"></param>
        public static void DisposeUnusedFilter(IReadOnlyList<FilterModel> newList, IReadOnlyList<FilterModel> oldList, OpenGlContext context)
        {
            var disable = context.Enable();

            foreach (var old in oldList)
            {
                // delete if the filter is not used in new list
                if(newList.All(newFilter => !ReferenceEquals(newFilter, old)))
                    old.Dispose();
            }

            if(disable)
                context.Disable();
        }

        protected virtual void OnChanged(bool[] changed)
        {
            Changed?.Invoke(this, new FiltersChangedEventArgs(changed));
        }
    }
}
