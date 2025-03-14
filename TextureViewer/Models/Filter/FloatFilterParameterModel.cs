﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextureViewer.ViewModels.Filter;

namespace TextureViewer.Models.Filter
{
    public class FloatFilterParameterModel : FilterParameterModel<float>, IFilterParameter
    {
        public class FloatParameterAction : ParameterAction
        {
            public FloatParameterAction(float value, ModificationType modType) : base(value, modType)
            {
            }

            public override float Invoke(float value)
            {
                switch (ModType)
                {
                    case ModificationType.Add:
                        return value + OpValue;
                    case ModificationType.Multiply:
                        return value * OpValue;
                    case ModificationType.Set:
                        return OpValue;
                }

                return value;
            }
        }

        public FloatFilterParameterModel(string name, string variableName, float min, float max, float defaultValue) 
            : base(name, variableName, min, max, defaultValue)
        {
            currentValue = defaultValue;

            // default actions
            Actions[ActionType.OnAdd] = new FloatParameterAction(1.0f, ModificationType.Add);
            Actions[ActionType.OnSubtract] = new FloatParameterAction(-1.0f, ModificationType.Add);
        }

        private float currentValue;
        public override float Value
        {
            get => currentValue;
            set
            {
                var clamped = Math.Min(Math.Max(value, Min), Max);
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (currentValue == clamped) return;

                currentValue = clamped;
                OnPropertyChanged(nameof(Value));
            }
        }

        public ParameterType GetParamterType()
        {
            return ParameterType.Float;
        }

        public FilterParameterModelBase GetBase()
        {
            return this;
        }

        public BoolFilterParameterModel GetBoolModel()
        {
            throw new InvalidCastException();
        }

        public IntFilterParameterModel GetIntModel()
        {
            throw new InvalidCastException();
        }

        public FloatFilterParameterModel GetFloatModel()
        {
            return this;
        }
    }
}
