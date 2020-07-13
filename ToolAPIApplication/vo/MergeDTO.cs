using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ToolAPIApplication.vo
{
    public class MergeDTO
    {
        public MergeDTO(string damageType, DamageResultVO damageResult)
        {
            this.damageType = damageType ?? throw new ArgumentNullException(nameof(damageType));
            this.damageResult = damageResult ?? throw new ArgumentNullException(nameof(damageResult));
        }

        public string damageType { get; set; }
        public DamageResultVO damageResult { get; set; }
    }


}
