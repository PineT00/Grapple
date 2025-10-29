using System.Collections.Generic;
using UnityEngine;

namespace PlantGenerator
{
    /// <summary>
    /// 브랜치 타입을 정의하는 템플릿 (프리팹에 부착)
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider))]
    public class BranchTemplate : MonoBehaviour
    {
        public const int MaxSockets = 4;

        [Header("성장 제약")]
        [Tooltip("이 브랜치가 생성될 수 있는 최소 깊이")]
        [SerializeField] int depthMin = 0;

        [Tooltip("이 브랜치가 생성될 수 있는 최대 깊이")]
        [SerializeField] int depthMax = 12;

        [Tooltip("이 브랜치 타입의 최대 개수")]
        [SerializeField] int maxCount = 500;

        [Tooltip("전체 식물에서 이 브랜치가 차지할 수 있는 최대 비율 (%)")]
        [SerializeField] [Range(0, 100)] float quotaPercent = 100f;

        [Tooltip("이 브랜치가 성장하기 전에 필요한 다른 브랜치 개수")]
        [SerializeField] int minTotalOtherBranches = 0;

        [Header("충돌 검사")]
        [Tooltip("이 레이어의 콜라이더를 피해서 성장")]
        [SerializeField] LayerMask obstacleLayers = 1;

        [Tooltip("이 레이어의 표면에만 붙어서 성장 (0 = 제약 없음)")]
        [SerializeField] LayerMask surfaceLayers = 0;

        [Tooltip("표면과의 최대 거리")]
        [SerializeField] float surfaceDistance = 1f;

        [Header("회전 파라미터")]
        [Tooltip("소켓 기준 랜덤 회전 각도 (Pitch/Yaw)")]
        [SerializeField] [Range(0, 180)] float maxPivotAngle = 30f;

        [Tooltip("Z축 기준 랜덤 롤 각도")]
        [SerializeField] [Range(0, 180)] float maxRollAngle = 30f;

        [Tooltip("위쪽으로 성장 편향 (-1: 아래, 0: 중립, 1: 위)")]
        [SerializeField] [Range(-1, 1)] float growUpwards = 0f;

        [Tooltip("브랜치의 Up 벡터를 항상 위로 향하게 함")]
        [SerializeField] bool faceUpwards = false;

        CapsuleCollider capsuleCollider;
        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        List<BranchSocket> sockets;

        public int DepthMin => depthMin;
        public int DepthMax => depthMax;
        public int MaxCount => maxCount;
        public float QuotaPercent => quotaPercent;
        public int MinTotalOtherBranches => minTotalOtherBranches;
        public LayerMask ObstacleLayers => obstacleLayers;
        public bool NeedsSurface => surfaceLayers != 0;
        public LayerMask SurfaceLayers => surfaceLayers;
        public float SurfaceDistance => surfaceDistance;
        public float MaxPivotAngle => maxPivotAngle;
        public float MaxRollAngle => maxRollAngle;
        public float GrowUpwards => growUpwards;
        public bool FaceUpwards => faceUpwards;
        public List<BranchSocket> Sockets => sockets;

        public CapsuleCollider Capsule
        {
            get
            {
                if (capsuleCollider == null)
                    capsuleCollider = GetComponent<CapsuleCollider>();
                return capsuleCollider;
            }
        }

        /// <summary>
        /// 자식 오브젝트에서 소켓 찾기
        /// </summary>
        public void FindSockets()
        {
            sockets = new List<BranchSocket>();
            foreach (Transform child in transform)
            {
                if (child.TryGetComponent(out BranchSocket socket))
                {
                    sockets.Add(socket);
                }
            }
        }

        /// <summary>
        /// 런타임에 브랜치 인스턴스 생성
        /// </summary>
        public Branch CreateBranch()
        {
            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();
            if (meshFilter == null)
                meshFilter = GetComponent<MeshFilter>();

            var gameObj = new GameObject(name, typeof(Branch), typeof(MeshFilter), typeof(MeshRenderer), typeof(CapsuleCollider))
            {
                layer = gameObject.layer,
                isStatic = true
            };

            var branch = gameObj.GetComponent<Branch>();

            // 메시 복사
            var newMeshFilter = gameObj.GetComponent<MeshFilter>();
            newMeshFilter.sharedMesh = meshFilter != null ? meshFilter.sharedMesh : null;

            // 렌더러 설정 복사
            var newRenderer = gameObj.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                newRenderer.sharedMaterials = meshRenderer.sharedMaterials;
                newRenderer.shadowCastingMode = meshRenderer.shadowCastingMode;
            }

            // 콜라이더 설정 복사
            var newCapsule = gameObj.GetComponent<CapsuleCollider>();
            var sourceCapsule = Capsule;
            newCapsule.center = sourceCapsule.center;
            newCapsule.direction = sourceCapsule.direction;
            newCapsule.height = sourceCapsule.height;
            newCapsule.radius = sourceCapsule.radius;
            newCapsule.isTrigger = false;

            return branch;
        }
    }
}
