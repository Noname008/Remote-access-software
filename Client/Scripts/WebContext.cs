using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Client.Scripts
{
    internal class WebContext
    {
        Dictionary<Commands, MethodInfo> mapControler = new Dictionary<Commands, MethodInfo>();
        Type type;
        public WebContext(Type type)
        {
            this.type = type;
            foreach (var i in type.GetMethods())
            {
                foreach (var atr in i.GetCustomAttributes(true))
                {
                    if (atr.GetType() == typeof(MethodAttribute))
                    {
                        mapControler.Add(((MethodAttribute)atr).Name, i);
                    }
                }
            }
        }

        protected void Invoke(Commands type, object[] data)
        {
            mapControler[type].Invoke(Activator.CreateInstance(this.type), data);
        }

        public void Invoke(Commands type, String data) => Invoke(type, new object[] { data });
    }

    [AttributeUsage(AttributeTargets.Method,
                           AllowMultiple = false)]
    public class MethodAttribute : Attribute
    {
        public Commands Name { get; set; }
        public MethodAttribute(Commands name)
        {
            this.Name = name;
        }
    }
}