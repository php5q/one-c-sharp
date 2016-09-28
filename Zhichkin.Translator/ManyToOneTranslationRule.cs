﻿using System;
using Zhichkin.ChangeTracking;
using System.Collections.Generic;

namespace Zhichkin.Integrator.Translator
{
    public class ManyToOneTranslationRule : OneToOneTranslationRule
    {
        public string TypeCodeField = string.Empty;
        public string ObjectField = string.Empty;
        public object Value = null;
        public int TypeCodeValue = 0;
        public int TestTypeCode = 0;
        private bool value_is_set = false;
        private bool type_code_is_set = false;
        public override void Apply(ChangeTrackingField sourceField, object sourceValue, IList<ChangeTrackingField> targetFields, IList<object> targetValues)
        {
            if (sourceField.Name == ObjectField)
            {
                Value = sourceValue;
                value_is_set = true;
            }
            else if (sourceField.Name == TypeCodeField)
            {
                TypeCodeValue = Utilities.GetInt32((byte[])sourceValue);
                type_code_is_set = true;
            }
            else // _TYPE
            {
                return; // allways 0x08 the rest values are ignored
            }
            if (value_is_set && type_code_is_set)
            {
                targetFields.Add(new ChangeTrackingField()
                {
                    Name = Name,
                    Type = "binary", // binary(16)
                    IsKey = sourceField.IsKey
                });
                if (TestTypeCode == TypeCodeValue) // TEST: byte[4] ?
                {
                    targetValues.Add(Value);
                }
                else
                {
                    targetValues.Add(Guid.Empty); // TEST: byte[16] ?
                }
            }
        }
    }
}