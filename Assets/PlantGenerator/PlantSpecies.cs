using UnityEngine;

namespace PlantGenerator
{
    /// <summary>
    /// 식물 종 데이터 (ScriptableObject)
    /// </summary>
    [CreateAssetMenu(fileName = "New Plant Species", menuName = "Plant Generator/Plant Species")]
    public class PlantSpecies : ScriptableObject
    {
        [Tooltip("루트 브랜치 템플릿")]
        [SerializeField] BranchTemplate rootBranch;

        [Tooltip("전체 브랜치 최대 개수")]
        [SerializeField] int maxTotalBranches = 50;

        public BranchTemplate RootBranch => rootBranch;
        public int MaxTotalBranches => maxTotalBranches;
    }
}
