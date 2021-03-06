﻿using Jint.Collections;
using Jint.Native.Symbol;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;

namespace Jint.Native.Iterator
{
    internal sealed class IteratorPrototype : IteratorInstance
    {
        private string _name;

        private IteratorPrototype(Engine engine) : base(engine)
        {
        }

        public static IteratorPrototype CreatePrototypeObject(Engine engine, string name, IteratorConstructor iteratorConstructor)
        {
            var obj = new IteratorPrototype(engine)
            {
                _prototype = engine.Object.PrototypeObject,
                _name = name
            };

            return obj;
        }

        protected override void Initialize()
        {
            var properties = new StringDictionarySlim<PropertyDescriptor>(3)
            {
                ["name"] = new PropertyDescriptor("Map", PropertyFlag.Configurable),
                ["next"] = new PropertyDescriptor(new ClrFunctionInstance(Engine, "next", Next, 0, PropertyFlag.Configurable), true, false, true)
            };
            if (_name != null)
            {
                properties[GlobalSymbolRegistry.ToStringTag] = new PropertyDescriptor(_name, PropertyFlag.Configurable);
            }
            SetProperties(properties, hasSymbols: true);
        }

        private JsValue Next(JsValue thisObj, JsValue[] arguments)
        {
            if (!(thisObj is IteratorInstance iterator))
            {
                return ExceptionHelper.ThrowTypeError<JsValue>(_engine);
            }

            return iterator.Next();
        }
    }
}