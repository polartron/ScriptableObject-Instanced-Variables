﻿using System;
using UnityEngine;

namespace Fasteraune.SO.Instances.Variables
{
    [Serializable]
    public abstract class VariableReference
    {

        public ReferenceType Type = ReferenceType.Constant;
        public InstanceOwner Connection;
    }

    [Serializable]
    public class VariableReference<TVariableType, TVariable> : VariableReference, IDisposable where TVariable : Variable<TVariableType>
    {
        public TVariableType ConstantValue;
        public TVariable Variable;

        private event Action<TVariableType> OnConstantValueChanged;
        private Variable<TVariableType> instancedVariable;

        private Variable<TVariableType> InstancedVariable
        {
            get
            {
                if (instancedVariable == null)
                {
                    var connection = Connection.Parent ? Connection.Parent : Connection;
                    instancedVariable = Variable.GetOrCreateInstance(connection) as Variable<TVariableType>;
                }

                return instancedVariable;
            }
        }

        private bool caching = false;
        private TVariableType cachedVariable;

        public VariableReference()
        {
        }

        public VariableReference(TVariableType value) : base()
        {
            Value = value;
        }

        public static implicit operator TVariableType(VariableReference<TVariableType, TVariable> reference)
        {
            return reference.Value;
        }

        public virtual TVariableType Value
        {
            get
            {
                switch (Type)
                {
                    case ReferenceType.Constant:
                    {
                        return ConstantValue;
                    }

                    case ReferenceType.Shared:
                    {
                        if (caching)
                        {
                            return cachedVariable;
                        }
                        
                        if (VariableReferenceMissing())
                        {
                            return default(TVariableType);
                        }

                        return Variable.Value;
                    }

                    case ReferenceType.Instanced:
                    {
                        if (VariableReferenceMissing() || ConnectionReferenceMissing())
                        {
                            return default(TVariableType);
                        }
                        
                        if (caching)
                        {
                            return cachedVariable;
                        }

                        return InstancedVariable.Value;
                    }

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            set
            {
                switch (Type)
                {
                    case ReferenceType.Constant:
                    {
                        ConstantValue = value;

                        OnConstantValueChanged?.Invoke(value);

                        break;
                    }

                    case ReferenceType.Shared:
                    {
                        if (VariableReferenceMissing())
                        {
                            return;
                        }

                        Variable.Value = value;

                        break;
                    }

                    case ReferenceType.Instanced:
                    {
                        if (VariableReferenceMissing() || ConnectionReferenceMissing())
                        {
                            return;
                        }

                        InstancedVariable.Value = value;

                        break;
                    }

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public virtual void AddListener(Action<TVariableType> listener)
        {
            switch (Type)
            {
                case ReferenceType.Constant:
                {
                    OnConstantValueChanged += listener;
                    break;
                }

                case ReferenceType.Shared:
                {
                    if (VariableReferenceMissing())
                    {
                        return;
                    }

                    Variable.OnValueChanged += listener;
                    break;
                }

                case ReferenceType.Instanced:
                {
                    if (VariableReferenceMissing() || ConnectionReferenceMissing())
                    {
                        return;
                    }

                    InstancedVariable.OnValueChanged += listener;
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public virtual void RemoveListener(Action<TVariableType> listener)
        {
            switch (Type)
            {
                case ReferenceType.Constant:
                {
                    OnConstantValueChanged -= listener;
                    break;
                }

                case ReferenceType.Shared:
                {
                    if (VariableReferenceMissing())
                    {
                        return;
                    }

                    Variable.OnValueChanged -= listener;
                    break;
                }

                case ReferenceType.Instanced:
                {
                    if (VariableReferenceMissing() || ConnectionReferenceMissing())
                    {
                        return;
                    }

                    InstancedVariable.OnValueChanged -= listener;
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool VariableReferenceMissing()
        {
            if (Variable == null)
            {
                Debug.LogError("Missing reference to variable asset");
                return true;
            }

            return false;
        }

        private bool ConnectionReferenceMissing()
        {
            if (Connection == null)
            {
                Debug.LogError("Missing reference to InstancedVariableOwner script");
                return true;
            }

            return false;
        }

        public void EnableCaching()
        {
            switch (Type)
            {
                case ReferenceType.Constant:
                    return;
                
                case ReferenceType.Shared:
                {
                    if (VariableReferenceMissing())
                    {
                        return;
                    }
                    
                    Variable.OnValueChanged += VariableOnOnValueChanged;
                    cachedVariable = Variable.Value;
                    break;
                }

                case ReferenceType.Instanced:
                {
                    if (VariableReferenceMissing() || ConnectionReferenceMissing())
                    {
                        return;
                    }

                    InstancedVariable.OnValueChanged += VariableOnOnValueChanged;
                    cachedVariable = InstancedVariable.Value;
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }

            caching = true;
        }

        private void VariableOnOnValueChanged(TVariableType value)
        {
            cachedVariable = value;
        }

        public void Dispose()
        {
            if (caching)
            {
                switch (Type)
                {
                    case ReferenceType.Constant:
                        return;
                    
                    case ReferenceType.Shared:
                    {
                        if (VariableReferenceMissing())
                        {
                            return;
                        }
                        
                        Variable.OnValueChanged -= VariableOnOnValueChanged;
                        break;
                    }
    
                    case ReferenceType.Instanced:
                    {
                        if (VariableReferenceMissing() || ConnectionReferenceMissing())
                        {
                            return;
                        }
    
                        InstancedVariable.OnValueChanged -= VariableOnOnValueChanged;
                        break;
                    }

                    default:
                        throw new ArgumentOutOfRangeException();
                }                
            }
        }
    }
}