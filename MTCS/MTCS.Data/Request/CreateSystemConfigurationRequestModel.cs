﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Request
{
    public class CreateSystemConfigurationRequestModel
    {
        public string ConfigKey { get; set; }

        public string ConfigValue { get; set; }
    }
}
