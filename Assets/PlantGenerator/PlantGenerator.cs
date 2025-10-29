using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlantGenerator
{
    /// <summary>
    /// 랜덤 식물 생성 메인 컨트롤러
    /// </summary>
    public class PlantGenerator : MonoBehaviour
    {
        const int MaxFailedGrowAttempts = 5000;
        const int GrowAttemptsPerFrame = 200;
        const int BranchesPerFrame = 20;
        static readonly Collider[] ColliderCache = new Collider[4];

        [Header("설정")]
        [Tooltip("식물 종 데이터")]
        [SerializeField] PlantSpecies species;

        [Tooltip("랜덤 시드")]
        [SerializeField] int seed;

        [Tooltip("성장 완료 후 콜라이더 유지")]
        [SerializeField] bool keepColliders = false;

        [Tooltip("성장 완료 후 Branch 컴포넌트 유지")]
        [SerializeField] bool keepBranchComponents = false;

        [Header("상태")]
        [SerializeField] PlantState state = PlantState.Done;

        Branch rootBranch;
        List<Branch> branches;
        Queue<Branch> branchesWithOpenSockets;
        List<BranchType> branchTypes;
        int nextSocketIndex;
        int failedAttemptsSinceBranchAdded;

        public PlantState State => state;
        public Branch RootBranch => rootBranch;

        void Update()
        {
            if (state == PlantState.Growing)
            {
                PrepareForGrowing();

                for (int i = 0; i < BranchesPerFrame; i++)
                {
                    Grow();
                }
            }
        }

        /// <summary>
        /// 식물 재생성
        /// </summary>
        [ContextMenu("Regrow Plant")]
        public void Regrow()
        {
            if (species == null || species.RootBranch == null)
            {
                Debug.LogError("PlantSpecies 또는 RootBranch가 설정되지 않았습니다.", this);
                state = PlantState.MissingData;
                return;
            }

            ResetPlant();
            state = PlantState.Growing;
        }

        void ResetPlant()
        {
            // 기존 브랜치 제거
            ClearChildren(transform);

            // 캐시 초기화
            ClearCache();
            PreprocessBranchType(species.RootBranch);

            // 루트 브랜치 생성
            CreateRootBranch();

            branchesWithOpenSockets.Enqueue(branches[0]);
            nextSocketIndex = 0;

            UpdateGrowableBranchTypes();

            UnityEngine.Random.InitState(seed);
            failedAttemptsSinceBranchAdded = 0;
        }

        void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        void ClearCache()
        {
            rootBranch = null;

            branchTypes = new List<BranchType>();
            branches = new List<Branch>();
            branchesWithOpenSockets = new Queue<Branch>();
        }

        void PrepareForGrowing()
        {
            if (species == null || species.RootBranch == null)
            {
                state = PlantState.MissingData;
                return;
            }

            if (branches == null || branches.Count == 0 || branches[0] == null)
            {
                ResetPlant();
            }

            state = PlantState.Growing;
        }

        void PreprocessBranchType(BranchTemplate template)
        {
            if (branchTypes.Any(bt => bt.Template == template))
                return;

            if (branchTypes.Count >= 32)
            {
                Debug.LogError("32개 이상의 브랜치 타입은 지원하지 않습니다.");
                return;
            }

            template.FindSockets();
            var branchType = new BranchType(template);
            branchTypes.Add(branchType);

            foreach (var socket in template.Sockets)
            {
                foreach (var option in socket.BranchOptions)
                {
                    if (option.Template != null)
                        PreprocessBranchType(option.Template);
                }
            }
        }

        void CreateRootBranch()
        {
            if (species?.RootBranch != null)
            {
                rootBranch = AddBranch(null, 0, branchTypes[0], Vector3.zero, Quaternion.Euler(-90, 0, 0));
            }
        }

        bool Grow()
        {
            int attempts = GrowAttemptsPerFrame;
            while (state == PlantState.Growing && attempts-- > 0)
            {
                var branch = FindBranchToGrow();
                if (branch != null)
                    return true;
            }

            if (failedAttemptsSinceBranchAdded > MaxFailedGrowAttempts)
            {
                OnGrowComplete();
            }

            return false;
        }

        Branch FindBranchToGrow()
        {
            if (branches.Count >= species.MaxTotalBranches)
            {
                OnGrowComplete();
                return null;
            }

            if (branchesWithOpenSockets.Count == 0)
            {
                OnGrowComplete();
                return null;
            }

            var parent = branchesWithOpenSockets.Peek();
            BranchSocket openSocket = null;

            while (openSocket == null)
            {
                if (nextSocketIndex < parent.Template.Sockets.Count)
                {
                    if (parent.Children[nextSocketIndex] == null)
                    {
                        openSocket = parent.Template.Sockets[nextSocketIndex];
                    }
                    else
                    {
                        nextSocketIndex++;
                    }
                }
                else
                {
                    nextSocketIndex = 0;
                    branchesWithOpenSockets.Dequeue();
                    if (parent != null)
                        branchesWithOpenSockets.Enqueue(parent);

                    parent = branchesWithOpenSockets.Peek();
                }
            }

            if (parent == null)
            {
                Debug.LogWarning("Null 브랜치 발견. 재시작합니다.");
                ResetPlant();
                state = PlantState.Growing;
                return null;
            }

            int depth = parent.Depth + 1;
            var growableBranchTypes = new List<(BranchType branchType, float weight)>();

            foreach (var bt in branchTypes)
            {
                if (bt.Growable &&
                    bt.Template.DepthMin <= depth &&
                    bt.Template.DepthMax >= depth &&
                    openSocket.ContainsBranchOption(bt.Template, out float weight))
                {
                    growableBranchTypes.Add((bt, weight));
                }
            }

            if (growableBranchTypes.Count == 0)
            {
                nextSocketIndex++;
                OnGrowFailed();
                return null;
            }

            // 가중치 기반 랜덤 선택
            var pair = PickWeighted(growableBranchTypes, tuple => tuple.weight);
            var branchType = pair.branchType;
            var template = branchType.Template;

            GetSocketPositionAndRotation(parent, nextSocketIndex, out Vector3 socketLocalPos, out Quaternion socketLocalRot);

            // 랜덤 회전
            float xRot = UnityEngine.Random.Range(-template.MaxPivotAngle, template.MaxPivotAngle);
            float yRot = UnityEngine.Random.Range(-template.MaxPivotAngle, template.MaxPivotAngle);
            float zRot = UnityEngine.Random.Range(-template.MaxRollAngle, template.MaxRollAngle);

            Quaternion pivot = Quaternion.Euler(xRot, yRot, 0);
            Quaternion globalRot = parent.transform.rotation * socketLocalRot * pivot;
            Vector3 globalPos = parent.transform.TransformPoint(socketLocalPos);

            // 위쪽/아래쪽 성장 편향
            if (template.GrowUpwards < 0)
            {
                var down = Quaternion.LookRotation(Vector3.down, globalRot * Vector3.forward);
                globalRot = Quaternion.SlerpUnclamped(globalRot, down, -template.GrowUpwards);
            }
            else if (template.GrowUpwards > 0)
            {
                var up = Quaternion.LookRotation(Vector3.up, globalRot * Vector3.back);
                globalRot = Quaternion.SlerpUnclamped(globalRot, up, template.GrowUpwards);
            }

            if (template.FaceUpwards)
            {
                globalRot = Quaternion.LookRotation(globalRot * Vector3.forward, Vector3.up);
            }

            // Roll 적용
            Quaternion roll = Quaternion.Euler(0, 0, zRot);
            globalRot *= roll;

            // 배치 가능 여부 확인
            if (CheckPlacement(globalPos, globalRot, template, parent.gameObject))
            {
                var branch = AddBranch(parent, nextSocketIndex, branchType,
                    socketLocalPos, Quaternion.Inverse(parent.transform.rotation) * globalRot);

                Physics.SyncTransforms();

                if (branch.HasOpenSockets())
                {
                    branchesWithOpenSockets.Enqueue(branch);
                }

                if (!parent.HasOpenSockets())
                {
                    nextSocketIndex = 0;
                    branchesWithOpenSockets.Dequeue();
                }

                UpdateGrowableBranchTypes();
                OnGrowSuccess();
                return branch;
            }

            nextSocketIndex++;
            OnGrowFailed();
            return null;
        }

        bool CheckPlacement(Vector3 globalPos, Quaternion globalRot, BranchTemplate template, GameObject ignoredParent)
        {
            if (!CheckIfAreaClear(globalPos, globalRot, template, ignoredParent))
                return false;

            if (template.NeedsSurface && !CheckIfTouchesSurface(globalPos, globalRot, template))
                return false;

            return true;
        }

        /// <summary>
        /// 충돌 영역이 비어있는지 확인
        /// </summary>
        static bool CheckIfAreaClear(Vector3 globalPos, Quaternion globalRot, BranchTemplate template, GameObject ignoredParent)
        {
            float radius = template.Capsule.radius;
            float height = template.Capsule.height;

            float startDist = Mathf.Min(height - radius, 3 * radius);
            float endDist = Mathf.Max(startDist, height - radius);

            Vector3 dir = globalRot * Vector3.forward;
            Vector3 start = globalPos + dir * startDist;
            Vector3 end = globalPos + dir * endDist;

            LayerMask occupied = template.ObstacleLayers;

            if (ignoredParent == null)
            {
                return !Physics.CheckCapsule(start, end, radius, occupied, QueryTriggerInteraction.Ignore);
            }

            int count = Physics.OverlapCapsuleNonAlloc(start, end, radius, ColliderCache, occupied, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (ColliderCache[i].gameObject != ignoredParent)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 표면에 닿아있는지 확인 (담쟁이 덩굴용)
        /// </summary>
        bool CheckIfTouchesSurface(Vector3 globalPos, Quaternion globalRot, BranchTemplate template)
        {
            float radius = template.Capsule.radius;
            float height = template.Capsule.height;
            Vector3 dir = globalRot * Vector3.forward;

            Vector3 offset = globalRot * Vector3.down * radius;
            Vector3 start = globalPos + offset + dir * (0.5f * height);
            Vector3 end = globalPos + offset + dir * (height - radius);

            radius *= template.SurfaceDistance;

            int count = Physics.OverlapCapsuleNonAlloc(start, end, radius, ColliderCache,
                template.SurfaceLayers, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                var collider = ColliderCache[i];

                // 자기 자신의 브랜치는 제외
                if (collider.TryGetComponent(out Branch branch))
                {
                    var owner = branch.GetComponentInParent<PlantGenerator>();
                    if (owner == this)
                        continue;
                }

                return true;
            }

            return false;
        }

        void GetSocketPositionAndRotation(Branch parent, int socketIndex, out Vector3 socketLocalPos, out Quaternion socketLocalRot)
        {
            if (socketIndex >= parent.Template.Sockets.Count)
            {
                Debug.LogError($"존재하지 않는 소켓 {socketIndex + 1} on {parent.Template.name}");
                socketLocalPos = Vector3.zero;
                socketLocalRot = Quaternion.identity;
                return;
            }

            var socket = parent.Template.Sockets[socketIndex];
            var socketTransform = socket.transform;
            socketLocalPos = socketTransform.localPosition;
            socketLocalRot = socketTransform.localRotation;
        }

        Branch AddBranch(Branch parent, int socketIndex, BranchType branchType, Vector3 localPosition, Quaternion localRotation)
        {
            var branch = branchType.Template.CreateBranch();
            var branchTransform = branch.transform;

            if (parent != null)
            {
                parent.Children[socketIndex] = branch;
                branch.transform.SetParent(parent.transform, false);
                branch.Depth = parent.Depth + 1;
            }
            else
            {
                branchTransform.SetParent(transform, false);
            }

            branchTransform.SetLocalPositionAndRotation(localPosition, localRotation);
            branch.name = $"D{branch.Depth} {branchType.Template.name}";
            branch.Template = branchType.Template;

            branches.Add(branch);
            branchType.TotalCount++;
            return branch;
        }

        void UpdateGrowableBranchTypes()
        {
            bool anyGrowable = false;

            foreach (var branchType in branchTypes)
            {
                var template = branchType.Template;
                if (branches.Count >= template.MinTotalOtherBranches &&
                    branchType.TotalCount < template.MaxCount &&
                    (branchType.TotalCount + 1f) / (branches.Count + 1f) <= template.QuotaPercent / 100f)
                {
                    anyGrowable = true;
                    branchType.Growable = true;
                }
                else
                {
                    branchType.Growable = false;
                }
            }

            if (!anyGrowable)
            {
                OnGrowComplete();
            }
        }

        void OnGrowSuccess()
        {
            failedAttemptsSinceBranchAdded = 0;
        }

        void OnGrowFailed()
        {
            failedAttemptsSinceBranchAdded++;
        }

        void OnGrowComplete()
        {
            state = PlantState.Done;
            Debug.Log($"식물 생성 완료: {branches.Count}개 브랜치", this);

            if (!keepColliders || !keepBranchComponents)
            {
                CleanupOnDone();
            }
        }

        void CleanupOnDone()
        {
            foreach (var branch in branches)
            {
                if (branch == null)
                    continue;

                if (!keepColliders && branch.TryGetComponent(out CapsuleCollider capsule))
                {
                    DestroyImmediate(capsule);
                }

                if (!keepBranchComponents)
                {
                    DestroyImmediate(branch);
                }
            }

            if (!keepBranchComponents)
            {
                branches.Clear();
                rootBranch = null;
            }
        }

        /// <summary>
        /// 가중치 기반 랜덤 선택
        /// </summary>
        static T PickWeighted<T>(List<T> items, Func<T, float> weightSelector)
        {
            float totalWeight = items.Sum(weightSelector);
            float randomValue = UnityEngine.Random.Range(0f, totalWeight);

            float cumulative = 0f;
            foreach (var item in items)
            {
                cumulative += weightSelector(item);
                if (randomValue <= cumulative)
                    return item;
            }

            return items[items.Count - 1];
        }

        /// <summary>
        /// 브랜치 타입 추적용 내부 클래스
        /// </summary>
        public class BranchType
        {
            public readonly BranchTemplate Template;
            public int TotalCount { get; set; }
            public bool Growable { get; set; }

            public BranchType(BranchTemplate template)
            {
                Template = template;
            }
        }
    }

    public enum PlantState
    {
        MissingData,
        Growing,
        Done
    }
}
