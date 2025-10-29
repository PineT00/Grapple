using UnityEngine;

namespace PlantGenerator
{
    /// <summary>
    /// 런타임에 생성된 브랜치 인스턴스
    /// </summary>
    public class Branch : MonoBehaviour
    {
        [SerializeField] int depth = 0;
        [SerializeField] BranchTemplate template;
        [SerializeField] Branch[] children = new Branch[4];

        public int Depth
        {
            get => depth;
            set => depth = value;
        }

        public BranchTemplate Template
        {
            get => template;
            set => template = value;
        }

        public Branch[] Children => children;

        /// <summary>
        /// 비어있는 소켓이 있는지 확인
        /// </summary>
        public bool HasOpenSockets()
        {
            if (template == null || template.Sockets == null)
                return false;

            int depthOfChildren = depth + 1;

            for (int i = 0; i < template.Sockets.Count; i++)
            {
                if (children[i] == null)
                {
                    var socket = template.Sockets[i];
                    foreach (var option in socket.BranchOptions)
                    {
                        var childTemplate = option.Template;
                        if (childTemplate != null &&
                            depthOfChildren >= childTemplate.DepthMin &&
                            depthOfChildren <= childTemplate.DepthMax)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
