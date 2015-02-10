using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NotFoundMvc
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    class NoMvcNotFoundAttribute : Attribute
    {
    }
}
