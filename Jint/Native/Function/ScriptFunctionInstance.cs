﻿using System.Collections.Generic;
using Esprima.Ast;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Environments;
using Jint.Runtime.Interpreter;

namespace Jint.Native.Function
{
    public sealed class ScriptFunctionInstance : FunctionInstance, IConstructor
    {
        internal readonly JintFunctionDefinition _function;

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-13.2
        /// </summary>
        public ScriptFunctionInstance(
            Engine engine,
            IFunction functionDeclaration,
            LexicalEnvironment scope,
            bool strict)
            : this(engine, new JintFunctionDefinition(engine, functionDeclaration), scope, strict)
        {
        }

        internal ScriptFunctionInstance(
            Engine engine,
            JintFunctionDefinition function,
            LexicalEnvironment scope,
            bool strict)
            : base(engine, function._name, function._parameterNames, scope, strict)
        {
            _function = function;

            _prototype = _engine.Function.PrototypeObject;

            _length = new PropertyDescriptor(JsNumber.Create(function._length), PropertyFlag.Configurable);

            var proto = new ObjectInstanceWithConstructor(engine, this)
            {
                _prototype = _engine.Object.PrototypeObject
            };

            _prototypeDescriptor = new PropertyDescriptor(proto, PropertyFlag.OnlyWritable);

            if (strict)
            {
                DefineOwnProperty(KnownKeys.Caller, engine._getSetThrower);
                DefineOwnProperty(KnownKeys.Arguments, engine._getSetThrower);
            }
        }

        // for example RavenDB wants to inspect this
        public IFunction FunctionDeclaration => _function._function;

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-13.2.1
        /// </summary>
        /// <param name="thisArg"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public override JsValue Call(JsValue thisArg, JsValue[] arguments)
        {
            var strict = _strict || _engine._isStrict;
            using (new StrictModeScope(strict, true))
            {
                // setup new execution context http://www.ecma-international.org/ecma-262/5.1/#sec-10.4.3
                JsValue thisBinding;
                if (StrictModeScope.IsStrictModeCode)
                {
                    thisBinding = thisArg;
                }
                else if (thisArg.IsNullOrUndefined())
                {
                    thisBinding = _engine.Global;
                }
                else if (thisArg._type != InternalTypes.Object)
                {
                    thisBinding = TypeConverter.ToObject(_engine, thisArg);
                }
                else
                {
                    thisBinding = thisArg;
                }

                var localEnv = LexicalEnvironment.NewDeclarativeEnvironment(_engine, _scope);

                _engine.EnterExecutionContext(localEnv, localEnv, thisBinding);

                try
                {
                    var argumentsInstance = _engine.DeclarationBindingInstantiation(
                        DeclarationBindingType.FunctionCode,
                        _function._hoistingScope,
                        functionInstance: this,
                        arguments);

                    var result = _function._body.Execute();

                    var value = result.GetValueOrDefault().Clone();

                    argumentsInstance?.FunctionWasCalled();

                    if (result.Type == CompletionType.Throw)
                    {
                        ExceptionHelper.ThrowJavaScriptException(_engine, value, result);
                    }

                    if (result.Type == CompletionType.Return)
                    {
                        return value;
                    }
                }
                finally
                {
                    _engine.LeaveExecutionContext();
                }

                return Undefined;
            }
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-13.2.2
        /// </summary>
        public ObjectInstance Construct(JsValue[] arguments, JsValue newTarget)
        {
            var thisArgument = OrdinaryCreateFromConstructor(TypeConverter.ToObject(_engine, newTarget), _engine.Object.PrototypeObject);

            var result = Call(thisArgument, arguments).TryCast<ObjectInstance>();
            if (!ReferenceEquals(result, null))
            {
                return result;
            }

            return thisArgument;
        }

        private class ObjectInstanceWithConstructor : ObjectInstance
        {
            private PropertyDescriptor _constructor;

            public ObjectInstanceWithConstructor(Engine engine, ObjectInstance thisObj) : base(engine)
            {
                _constructor = new PropertyDescriptor(thisObj, PropertyFlag.NonEnumerable);
            }

            public override IEnumerable<KeyValuePair<Key, PropertyDescriptor>> GetOwnProperties()
            {
                if (_constructor != null)
                {
                    yield return new KeyValuePair<Key, PropertyDescriptor>(KnownKeys.Constructor, _constructor);
                }

                foreach (var entry in base.GetOwnProperties())
                {
                    yield return entry;
                }
            }

            public override PropertyDescriptor GetOwnProperty(in Key propertyName)
            {
                if (propertyName == KnownKeys.Constructor)
                {
                    return _constructor ?? PropertyDescriptor.Undefined;
                }

                return base.GetOwnProperty(propertyName);
            }

            protected internal override void SetOwnProperty(in Key propertyName, PropertyDescriptor desc)
            {
                if (propertyName == KnownKeys.Constructor)
                {
                    _constructor = desc;
                }
                else
                {
                    base.SetOwnProperty(propertyName, desc);
                }
            }

            public override bool HasOwnProperty(in Key propertyName)
            {
                if (propertyName == KnownKeys.Constructor)
                {
                    return _constructor != null;
                }

                return base.HasOwnProperty(propertyName);
            }

            public override void RemoveOwnProperty(in Key propertyName)
            {
                if (propertyName == KnownKeys.Constructor)
                {
                    _constructor = null;
                }
                else
                {
                    base.RemoveOwnProperty(propertyName);
                }
            }
        }
    }
}