using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ToolAPIApplication.dto
{
    public class MergeDTO
    {
        public MergeDTO(string damageType, DamageResultDTO damageResult)
        {
            this.damageType = damageType ?? throw new ArgumentNullException(nameof(damageType));
            this.damageResult = damageResult ?? throw new ArgumentNullException(nameof(damageResult));
        }

        public string damageType { get; set; }
        public DamageResultDTO damageResult { get; set; }
    }


}
