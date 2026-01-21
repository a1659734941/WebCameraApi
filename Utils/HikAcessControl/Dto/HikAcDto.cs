using System;
using System.Collections.Generic;
using System.Text;

namespace HikAcessControl
{
    class HikAcDto
    {
        public string HikAcIP {  get; set; }
        public string HikAcUserName { get; set; }
        public string HikAcPassword { get; set; }
        public ushort HikAcPort { get; set; }
    }
}
