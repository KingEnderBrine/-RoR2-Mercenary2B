using Assets.SkinMods.Mercenary2B;
using BepInEx.Logging;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mercenary2B
{
    public partial class Mercenary2BPlugin
    {
        private static GameObject swordAccessoryPrefab;
        public static GameObject SwordAccessoryPrefab => swordAccessoryPrefab ? swordAccessoryPrefab : (swordAccessoryPrefab = assetBundle.LoadAsset<GameObject>("Assets/Mercenary2B/Resources/Prefabs/SwordAccessoryPrefab.prefab"));
        
        private static GameObject skirtPrefab;
        public static GameObject SkirtPrefab => skirtPrefab ? skirtPrefab : (skirtPrefab = assetBundle.LoadAsset<GameObject>("Assets/Mercenary2B/Resources/Prefabs/SkirtPrefab.prefab"));

        public static SkinDef SkinDef { get; private set; }

        private static readonly Dictionary<GameObject, ModificationObjects> appliedModificatons = new Dictionary<GameObject, ModificationObjects>();

        private static Mercenary2BPlugin Instance { get; set; }
        private static ManualLogSource InstanceLogger => Instance?.Logger;

        partial void BeforeAwake()
        {
            Instance = this;

            On.RoR2.SkinDef.Apply += SkinDefApply;
        }

        static partial void MercBodyMercenary2BSkinAdded(SkinDef skinDef, GameObject bodyPrefab)
        {
            SkinDef = skinDef;
        }

        private static void SkinDefApply(On.RoR2.SkinDef.orig_Apply orig, SkinDef self, GameObject modelObject)
        {
            orig(self, modelObject);
            
            try
            {
                RemoveInvalidModelObjects();
                
                appliedModificatons.TryGetValue(modelObject, out var modificatons);

                if (self != SkinDef)
                {
                    if (modificatons != null)
                    {
                        appliedModificatons.Remove(modelObject);
                        ClearSkinModifications(modificatons);
                    }
                    return;
                }
                if (modificatons == null)
                {
                    appliedModificatons[modelObject] = ApplySkinModifications(modelObject);
                }
            }
            catch (Exception e)
            {
                InstanceLogger.LogWarning("An error occured while adding accessories to a Mercenary2B skin");
                InstanceLogger.LogError(e);
            }
        }

        private static void ClearSkinModifications(ModificationObjects modificatons)
        {
            Destroy(modificatons.swordAccessoryInstance);
            Destroy(modificatons.swordAccessoryArmature);
            Destroy(modificatons.swordAccessoryDynamicBone);

            Destroy(modificatons.skirtInstance);
            Destroy(modificatons.skirtArmature);
            Destroy(modificatons.skirtDynamicBone);

            Destroy(modificatons.pelvisDynamicBoneCollider);
            Destroy(modificatons.thighL1DynamicBoneCollider);
            Destroy(modificatons.thighL2DynamicBoneCollider);
            Destroy(modificatons.thighR1DynamicBoneCollider);
            Destroy(modificatons.thighR2DynamicBoneCollider);
            Destroy(modificatons.stomachDynamicBoneCollider);
        }

        private static ModificationObjects ApplySkinModifications(GameObject modelObject)
        {
            var characterModel = modelObject.GetComponent<CharacterModel>();

            var modificatons = new ModificationObjects();

            ApplySwordAccessoriesModifications(modelObject, modificatons, characterModel);
            ApplySkirtModifications(modelObject, modificatons, characterModel);

            return modificatons;
        }

        private static void ApplySkirtModifications(GameObject modelObject, ModificationObjects modificatons, CharacterModel characterModel)
        {
            var stomach = modelObject.transform.Find("MercArmature/ROOT/base/stomach");
            modificatons.skirtInstance = Instantiate(SkirtPrefab, modelObject.transform, false);

            modificatons.skirtArmature = modificatons.skirtInstance.transform.Find("SkirtArmature").gameObject;
            modificatons.skirtArmature.transform.SetParent(stomach, false);

            modificatons.stomachDynamicBoneCollider = stomach.gameObject.AddComponent<DynamicBoneCollider>();
            modificatons.stomachDynamicBoneCollider.m_Center = new Vector3(0, 0.7F, 0);
            modificatons.stomachDynamicBoneCollider.m_Height = 0.6F;
            modificatons.stomachDynamicBoneCollider.m_Direction = DynamicBoneCollider.Direction.X;
            modificatons.stomachDynamicBoneCollider.m_Bound = DynamicBoneCollider.Bound.Outside;

            var pelvis = modelObject.transform.Find("MercArmature/ROOT/base/pelvis");
            modificatons.pelvisDynamicBoneCollider = pelvis.gameObject.AddComponent<DynamicBoneCollider>();
            modificatons.pelvisDynamicBoneCollider.m_Center = new Vector3(0, 0.2F, -0.05F);
            modificatons.pelvisDynamicBoneCollider.m_Height = 0.54F;
            modificatons.pelvisDynamicBoneCollider.m_Radius = 0.25F;
            modificatons.pelvisDynamicBoneCollider.m_Direction = DynamicBoneCollider.Direction.X;
            modificatons.pelvisDynamicBoneCollider.m_Bound = DynamicBoneCollider.Bound.Outside;

            var thighl = pelvis.Find("thigh.l");
            modificatons.thighL1DynamicBoneCollider = thighl.gameObject.AddComponent<DynamicBoneCollider>();
            modificatons.thighL1DynamicBoneCollider.m_Center = new Vector3(-0.1F, 0.24F, 0.04F);
            modificatons.thighL1DynamicBoneCollider.m_Height = 0.6F;
            modificatons.thighL1DynamicBoneCollider.m_Radius = 0.1F;
            modificatons.thighL1DynamicBoneCollider.m_Direction = DynamicBoneCollider.Direction.Y;
            modificatons.thighL1DynamicBoneCollider.m_Bound = DynamicBoneCollider.Bound.Outside;

            modificatons.thighL2DynamicBoneCollider = thighl.gameObject.AddComponent<DynamicBoneCollider>();
            modificatons.thighL2DynamicBoneCollider.m_Center = new Vector3(0.1F, 0.24F, 0.04F);
            modificatons.thighL2DynamicBoneCollider.m_Height = 0.6F;
            modificatons.thighL2DynamicBoneCollider.m_Radius = 0.1F;
            modificatons.thighL2DynamicBoneCollider.m_Direction = DynamicBoneCollider.Direction.Y;
            modificatons.thighL2DynamicBoneCollider.m_Bound = DynamicBoneCollider.Bound.Outside;

            var thighr = pelvis.Find("thigh.r");
            modificatons.thighR1DynamicBoneCollider = thighr.gameObject.AddComponent<DynamicBoneCollider>();
            modificatons.thighR1DynamicBoneCollider.m_Center = new Vector3(-0.1F, 0.24F, 0.04F);
            modificatons.thighR1DynamicBoneCollider.m_Height = 0.6F;
            modificatons.thighR1DynamicBoneCollider.m_Radius = 0.1F;
            modificatons.thighR1DynamicBoneCollider.m_Direction = DynamicBoneCollider.Direction.Y;
            modificatons.thighR1DynamicBoneCollider.m_Bound = DynamicBoneCollider.Bound.Outside;

            modificatons.thighR2DynamicBoneCollider = thighr.gameObject.AddComponent<DynamicBoneCollider>();
            modificatons.thighR2DynamicBoneCollider.m_Center = new Vector3(0.1F, 0.24F, 0.04F);
            modificatons.thighR2DynamicBoneCollider.m_Height = 0.6F;
            modificatons.thighR2DynamicBoneCollider.m_Radius = 0.1F;
            modificatons.thighR2DynamicBoneCollider.m_Direction = DynamicBoneCollider.Direction.Y;
            modificatons.thighR2DynamicBoneCollider.m_Bound = DynamicBoneCollider.Bound.Outside;

            modificatons.skirtDynamicBone = modelObject.AddComponent<DynamicBone>();
            modificatons.skirtDynamicBone.m_Root = modificatons.skirtArmature.transform.GetChild(0);
            modificatons.skirtDynamicBone.m_Exclusions = new List<Transform> { modificatons.skirtDynamicBone.m_Root.Find("skirt.base") };
            modificatons.skirtDynamicBone.m_Damping = 0.5F;
            modificatons.skirtDynamicBone.m_Elasticity = 0.25F;
            modificatons.skirtDynamicBone.m_Stiffness = 0;
            modificatons.skirtDynamicBone.m_Inert = 0.75F;
            modificatons.skirtDynamicBone.m_Radius = 0.15F;
            modificatons.skirtDynamicBone.m_RadiusDistrib = new AnimationCurve(new Keyframe(0, 0.6F), new Keyframe(1, 1))
            {
                postWrapMode = WrapMode.Loop,
                preWrapMode = WrapMode.Loop
            };
            modificatons.skirtDynamicBone.m_Colliders = new List<DynamicBoneCollider>
            {
                modificatons.pelvisDynamicBoneCollider,
                modificatons.thighL1DynamicBoneCollider,
                modificatons.thighL2DynamicBoneCollider,
                modificatons.thighR1DynamicBoneCollider,
                modificatons.thighR2DynamicBoneCollider,
                modificatons.stomachDynamicBoneCollider
            };

            Array.Resize(ref characterModel.baseRendererInfos, characterModel.baseRendererInfos.Length + 1);

            var skirtRenderer = modificatons.skirtInstance.transform.Find("Skirt").GetComponent<SkinnedMeshRenderer>();
            characterModel.baseRendererInfos[characterModel.baseRendererInfos.Length - 1] = new CharacterModel.RendererInfo
            {
                renderer = skirtRenderer,
                ignoreOverlays = false,
                defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                defaultMaterial = skirtRenderer.sharedMaterial
            };
        }

        private static void ApplySwordAccessoriesModifications(GameObject modelObject, ModificationObjects modificatons, CharacterModel characterModel)
        {
            var swordBase = modelObject.transform.Find("MercArmature/ROOT/base/stomach/chest/SwingCenter/SwordBase");
            modificatons.swordAccessoryInstance = Instantiate(SwordAccessoryPrefab, modelObject.transform, false);

            modificatons.swordAccessoryArmature = modificatons.swordAccessoryInstance.transform.Find("SwordAccessoryArmature").gameObject;
            modificatons.swordAccessoryArmature.transform.SetParent(swordBase, false);

            modificatons.swordAccessoryDynamicBone = modelObject.AddComponent<DynamicBone>();
            modificatons.swordAccessoryDynamicBone.m_Root = modificatons.swordAccessoryArmature.transform.GetChild(0);
            modificatons.swordAccessoryDynamicBone.m_Force = new Vector3(0, -0.05F, 0);
            modificatons.swordAccessoryDynamicBone.m_Damping = 0;
            modificatons.swordAccessoryDynamicBone.m_Elasticity = 0.05F;
            modificatons.swordAccessoryDynamicBone.m_Stiffness = 0;
            modificatons.swordAccessoryDynamicBone.m_Inert = 0;

            Array.Resize(ref characterModel.baseRendererInfos, characterModel.baseRendererInfos.Length + 1);

            var swordAccessoryRenderer = modificatons.swordAccessoryInstance.transform.Find("SwordAccessory").GetComponent<SkinnedMeshRenderer>();
            characterModel.baseRendererInfos[characterModel.baseRendererInfos.Length - 1] = new CharacterModel.RendererInfo
            {
                renderer = swordAccessoryRenderer,
                ignoreOverlays = false,
                defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                defaultMaterial = swordAccessoryRenderer.sharedMaterial
            };
        }

        private static void RemoveInvalidModelObjects()
        {
            foreach (var modelObject in appliedModificatons.Keys.Where(el => !el).ToList())
            {
                appliedModificatons.Remove(modelObject);
            }
        }
    }
}
