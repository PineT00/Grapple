using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlantGenerator
{
    /// <summary>
    /// 브랜치가 연결될 수 있는 소켓
    /// </summary>
    public class BranchSocket : MonoBehaviour
    {
        [SerializeField] List<BranchSocketOption> branchOptions = new List<BranchSocketOption>();

        public IReadOnlyList<BranchSocketOption> BranchOptions => branchOptions;

        /// <summary>
        /// 특정 템플릿이 이 소켓에 사용 가능한지 확인
        /// </summary>
        public bool ContainsBranchOption(BranchTemplate template, out float weight)
        {
            var option = branchOptions.FirstOrDefault(o => o.Template == template);
            if (option != null)
            {
                weight = option.ProbabilityPercent;
                return true;
            }

            weight = 0;
            return false;
        }
    }
}
