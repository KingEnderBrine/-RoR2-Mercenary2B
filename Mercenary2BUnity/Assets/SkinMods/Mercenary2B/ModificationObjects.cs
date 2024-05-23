using UnityEngine;

namespace Mercenary2B
{
    public class ModificationObjects
    {
        public GameObject swordAccessoryInstance;
        public GameObject swordAccessoryArmature;
        public DynamicBone swordAccessoryDynamicBone;

        public GameObject skirtInstance;
        public GameObject skirtArmature;
        public DynamicBone skirtDynamicBone;

        public DynamicBoneCollider pelvisDynamicBoneCollider;
        public DynamicBoneCollider thighL1DynamicBoneCollider;
        public DynamicBoneCollider thighL2DynamicBoneCollider;
        public DynamicBoneCollider thighR1DynamicBoneCollider;
        public DynamicBoneCollider thighR2DynamicBoneCollider;
        public DynamicBoneCollider stomachDynamicBoneCollider;

        public SkinnedMeshRenderer swordMeshRenderer;
        public SkinnedMeshRenderer mercMeshRenderer;
        
        public GameObject breastBonesInstance;
        public DynamicBone mercBreastDynamicBone;

        public GameObject buttBonesInstance;
        public DynamicBone mercButtDynamicBone;

        public GameObject swordPointLight;
        public GameObject chestPointLight;
    }
}
