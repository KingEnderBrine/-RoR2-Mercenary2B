using BepInEx.Configuration;
using BepInEx.Logging;
using MonoMod.RuntimeDetour;
using RoR2;
using RoR2.ContentManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Mercenary2B
{
    public partial class Mercenary2BPlugin
    {
        private delegate IEnumerator ApplyAsyncHandler(SkinDef self, GameObject modelObject, List<AssetReferenceT<Material>> loadedMaterials, List<AssetReferenceT<Mesh>> loadedMeshes, AsyncReferenceHandleUnloadType unloadType);
        private delegate IEnumerator ApplyAsyncHookHandler(ApplyAsyncHandler orig, SkinDef self, GameObject modelObject, List<AssetReferenceT<Material>> loadedMaterials, List<AssetReferenceT<Mesh>> loadedMeshes, AsyncReferenceHandleUnloadType unloadType);

        private static GameObject swordAccessoryPrefab;
        public static GameObject SwordAccessoryPrefab => swordAccessoryPrefab ? swordAccessoryPrefab : (swordAccessoryPrefab = assetBundle.LoadAsset<GameObject>("Assets/Mercenary2B/Resources/Prefabs/SwordAccessoryPrefab.prefab"));
        
        private static GameObject skirtPrefab;
        public static GameObject SkirtPrefab => skirtPrefab ? skirtPrefab : (skirtPrefab = assetBundle.LoadAsset<GameObject>("Assets/Mercenary2B/Resources/Prefabs/SkirtPrefab.prefab"));
        
        private static GameObject breastBonesPrefab;
        public static GameObject BreastBonesPrefab => breastBonesPrefab ? breastBonesPrefab : (breastBonesPrefab = assetBundle.LoadAsset<GameObject>("Assets/Mercenary2B/Resources/Prefabs/breast_base.prefab"));

        private static GameObject buttBonesPrefab;
        public static GameObject ButtBonesPrefab => buttBonesPrefab ? buttBonesPrefab : (buttBonesPrefab = assetBundle.LoadAsset<GameObject>("Assets/Mercenary2B/Resources/Prefabs/butt_base.prefab"));

        private const int breastBoneIndex = 64;
        private const int buttBoneIndex = 76;

        public static SkinDef SkinDef { get; private set; }

        private static readonly ConditionalWeakTable<GameObject, ModificationObjects> appliedModifications = new ConditionalWeakTable<GameObject, ModificationObjects>();
        private static ConfigEntry<bool> DisableSkirt;

        partial void BeforeStart()
        {
            new Hook(typeof(SkinDef).GetMethod(nameof(SkinDef.ApplyAsync)), (ApplyAsyncHookHandler)SkinDefApplyAsync).Apply();
            DisableSkirt = Config.Bind("Main", "DisableSkirt", false, "Disable skirt on the skin");
        }

        static partial void MercBodyMercenary2BSkinAdded(SkinDef skinDef, GameObject bodyPrefab)
        {
            SkinDef = skinDef;
        }

        private static IEnumerator SkinDefApplyAsync(ApplyAsyncHandler orig, SkinDef self, GameObject modelObject, List<AssetReferenceT<Material>> loadedMaterials, List<AssetReferenceT<Mesh>> loadedMeshes, AsyncReferenceHandleUnloadType unloadType)
        {
            var enumerator = orig(self, modelObject, loadedMaterials, loadedMeshes, unloadType);
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
            
            try
            {
                appliedModifications.TryGetValue(modelObject, out var modifications);

                if (self != SkinDef)
                {
                    if (modifications != null)
                    {
                        appliedModifications.Remove(modelObject);
                        ClearSkinModifications(modifications);
                    }
                    yield break;
                }

                if (modifications == null)
                {
                    appliedModifications.Add(modelObject, ApplySkinModifications(modelObject));
                }
            }
            catch (Exception e)
            {
                InstanceLogger.LogWarning("An error occurred while adding accessories to a Mercenary2B skin");
                InstanceLogger.LogError(e);
            }
        }

        private static void ClearSkinModifications(ModificationObjects modifications)
        {
            Destroy(modifications.swordAccessoryInstance);
            Destroy(modifications.swordAccessoryArmature);
            Destroy(modifications.swordAccessoryDynamicBone);

            if (!DisableSkirt.Value)
            {
                Destroy(modifications.skirtInstance);
                Destroy(modifications.skirtArmature);
                Destroy(modifications.skirtDynamicBone);
            }

            Destroy(modifications.pelvisDynamicBoneCollider);
            Destroy(modifications.thighL1DynamicBoneCollider);
            Destroy(modifications.thighL2DynamicBoneCollider);
            Destroy(modifications.thighR1DynamicBoneCollider);
            Destroy(modifications.thighR2DynamicBoneCollider);
            Destroy(modifications.stomachDynamicBoneCollider);

            var oldBones = modifications.mercMeshRenderer.bones.ToList();
            oldBones.RemoveRange(buttBoneIndex, 3);
            oldBones.RemoveRange(breastBoneIndex, 3);
            modifications.mercMeshRenderer.bones = oldBones.ToArray();
            modifications.swordMeshRenderer.bones = oldBones.ToArray();

            Destroy(modifications.mercBreastDynamicBone);
            Destroy(modifications.breastBonesInstance);

            Destroy(modifications.mercButtDynamicBone);
            Destroy(modifications.buttBonesInstance);

            if (modifications.chestPointLight)
            {
                modifications.chestPointLight.SetActive(true);
            }
            if (modifications.swordPointLight)
            {
                modifications.swordPointLight.SetActive(true);
            }
        }

        private static ModificationObjects ApplySkinModifications(GameObject modelObject)
        {
            var characterModel = modelObject.GetComponent<CharacterModel>();

            var modifications = new ModificationObjects
            {
                mercMeshRenderer = modelObject.transform.Find("MercMesh").GetComponent<SkinnedMeshRenderer>(),
                swordMeshRenderer = modelObject.transform.Find("MercSwordMesh").GetComponent<SkinnedMeshRenderer>()
            };

            ApplyBreastModifications(modelObject, modifications);
            ApplyButtModifications(modelObject, modifications);
            ApplySwordAccessoriesModifications(modelObject, modifications, characterModel);
            if (!DisableSkirt.Value)
            {
                ApplySkirtModifications(modelObject, modifications, characterModel);
            }

            return modifications;
        }

        private static void ApplyBreastModifications(GameObject modelObject, ModificationObjects modifications)
        {
            var chest = modelObject.transform.Find("MercArmature/ROOT/base/stomach/chest");

            modifications.chestPointLight = (chest.Find("Point Light (1)") ?? chest.Find("Point Light"))?.gameObject;
            if (modifications.chestPointLight)
            {
                modifications.chestPointLight.SetActive(false);
            }

            modifications.breastBonesInstance = Instantiate(BreastBonesPrefab, chest, false);

            var newBones = modifications.mercMeshRenderer.bones.ToList();
            newBones.InsertRange(
                breastBoneIndex,
                new[]
                { 
                    modifications.breastBonesInstance.transform,
                    modifications.breastBonesInstance.transform.GetChild(0),
                    modifications.breastBonesInstance.transform.GetChild(1)
                });

            modifications.mercMeshRenderer.bones = newBones.ToArray();
            modifications.swordMeshRenderer.bones = newBones.ToArray();

            modifications.mercBreastDynamicBone = modelObject.AddComponent<DynamicBone>();
            modifications.mercBreastDynamicBone.m_Root = modifications.breastBonesInstance.transform;
            modifications.mercBreastDynamicBone.m_Damping = 0.2F;
            modifications.mercBreastDynamicBone.m_Elasticity = 0.05F;
            modifications.mercBreastDynamicBone.m_Stiffness = 0.8F;
            modifications.mercBreastDynamicBone.m_Inert = 0.5F;
        }

        private static void ApplyButtModifications(GameObject modelObject, ModificationObjects modifications)
        {
            var pelvis = modelObject.transform.Find("MercArmature/ROOT/base/pelvis");
            modifications.buttBonesInstance = Instantiate(ButtBonesPrefab, pelvis, false);

            var newBones = modifications.mercMeshRenderer.bones.ToList();
            newBones.InsertRange(
                buttBoneIndex,
                new[]
                {
                    modifications.buttBonesInstance.transform,
                    modifications.buttBonesInstance.transform.GetChild(0),
                    modifications.buttBonesInstance.transform.GetChild(1)
                });

            modifications.mercMeshRenderer.bones = newBones.ToArray();
            modifications.swordMeshRenderer.bones = newBones.ToArray();

            modifications.mercButtDynamicBone = modelObject.AddComponent<DynamicBone>();
            modifications.mercButtDynamicBone.m_Root = modifications.buttBonesInstance.transform;
            modifications.mercButtDynamicBone.m_Damping = 0.2F;
            modifications.mercButtDynamicBone.m_Elasticity = 0.05F;
            modifications.mercButtDynamicBone.m_Stiffness = 0.925F;
            modifications.mercButtDynamicBone.m_Inert = 0.5F;
        }

        private static void ApplySkirtModifications(GameObject modelObject, ModificationObjects modifications, CharacterModel characterModel)
        {
            var stomach = modelObject.transform.Find("MercArmature/ROOT/base/stomach");
            modifications.skirtInstance = Instantiate(SkirtPrefab, modelObject.transform, false);

            modifications.skirtArmature = modifications.skirtInstance.transform.Find("SkirtArmature").gameObject;
            modifications.skirtArmature.transform.SetParent(stomach, false);

            modifications.stomachDynamicBoneCollider = stomach.gameObject.AddComponent<DynamicBoneCollider>();
            modifications.stomachDynamicBoneCollider.m_Center = new Vector3(0, 0.7F, 0);
            modifications.stomachDynamicBoneCollider.m_Height = 0.6F;
            modifications.stomachDynamicBoneCollider.m_Direction = DynamicBoneCollider.Direction.X;
            modifications.stomachDynamicBoneCollider.m_Bound = DynamicBoneCollider.Bound.Outside;

            var pelvis = modelObject.transform.Find("MercArmature/ROOT/base/pelvis");
            modifications.pelvisDynamicBoneCollider = pelvis.gameObject.AddComponent<DynamicBoneCollider>();
            modifications.pelvisDynamicBoneCollider.m_Center = new Vector3(0, 0.2F, -0.05F);
            modifications.pelvisDynamicBoneCollider.m_Height = 0.54F;
            modifications.pelvisDynamicBoneCollider.m_Radius = 0.25F;
            modifications.pelvisDynamicBoneCollider.m_Direction = DynamicBoneCollider.Direction.X;
            modifications.pelvisDynamicBoneCollider.m_Bound = DynamicBoneCollider.Bound.Outside;

            var thighl = pelvis.Find("thigh.l");
            modifications.thighL1DynamicBoneCollider = thighl.gameObject.AddComponent<DynamicBoneCollider>();
            modifications.thighL1DynamicBoneCollider.m_Center = new Vector3(-0.1F, 0.24F, 0.04F);
            modifications.thighL1DynamicBoneCollider.m_Height = 0.6F;
            modifications.thighL1DynamicBoneCollider.m_Radius = 0.1F;
            modifications.thighL1DynamicBoneCollider.m_Direction = DynamicBoneCollider.Direction.Y;
            modifications.thighL1DynamicBoneCollider.m_Bound = DynamicBoneCollider.Bound.Outside;

            modifications.thighL2DynamicBoneCollider = thighl.gameObject.AddComponent<DynamicBoneCollider>();
            modifications.thighL2DynamicBoneCollider.m_Center = new Vector3(0.1F, 0.24F, 0.04F);
            modifications.thighL2DynamicBoneCollider.m_Height = 0.6F;
            modifications.thighL2DynamicBoneCollider.m_Radius = 0.1F;
            modifications.thighL2DynamicBoneCollider.m_Direction = DynamicBoneCollider.Direction.Y;
            modifications.thighL2DynamicBoneCollider.m_Bound = DynamicBoneCollider.Bound.Outside;

            var thighr = pelvis.Find("thigh.r");
            modifications.thighR1DynamicBoneCollider = thighr.gameObject.AddComponent<DynamicBoneCollider>();
            modifications.thighR1DynamicBoneCollider.m_Center = new Vector3(-0.1F, 0.24F, 0.04F);
            modifications.thighR1DynamicBoneCollider.m_Height = 0.6F;
            modifications.thighR1DynamicBoneCollider.m_Radius = 0.1F;
            modifications.thighR1DynamicBoneCollider.m_Direction = DynamicBoneCollider.Direction.Y;
            modifications.thighR1DynamicBoneCollider.m_Bound = DynamicBoneCollider.Bound.Outside;

            modifications.thighR2DynamicBoneCollider = thighr.gameObject.AddComponent<DynamicBoneCollider>();
            modifications.thighR2DynamicBoneCollider.m_Center = new Vector3(0.1F, 0.24F, 0.04F);
            modifications.thighR2DynamicBoneCollider.m_Height = 0.6F;
            modifications.thighR2DynamicBoneCollider.m_Radius = 0.1F;
            modifications.thighR2DynamicBoneCollider.m_Direction = DynamicBoneCollider.Direction.Y;
            modifications.thighR2DynamicBoneCollider.m_Bound = DynamicBoneCollider.Bound.Outside;

            modifications.skirtDynamicBone = modelObject.AddComponent<DynamicBone>();
            modifications.skirtDynamicBone.m_Root = modifications.skirtArmature.transform.GetChild(0);
            modifications.skirtDynamicBone.m_Exclusions = new List<Transform> { modifications.skirtDynamicBone.m_Root.Find("skirt.base") };
            modifications.skirtDynamicBone.m_Damping = 0.5F;
            modifications.skirtDynamicBone.m_Elasticity = 0.25F;
            modifications.skirtDynamicBone.m_Stiffness = 0;
            modifications.skirtDynamicBone.m_Inert = 0.75F;
            modifications.skirtDynamicBone.m_Radius = 0.15F;
            modifications.skirtDynamicBone.m_RadiusDistrib = new AnimationCurve(new Keyframe(0, 0.6F), new Keyframe(1, 1))
            {
                postWrapMode = WrapMode.Loop,
                preWrapMode = WrapMode.Loop
            };
            modifications.skirtDynamicBone.m_Colliders = new List<DynamicBoneCollider>
            {
                modifications.pelvisDynamicBoneCollider,
                modifications.thighL1DynamicBoneCollider,
                modifications.thighL2DynamicBoneCollider,
                modifications.thighR1DynamicBoneCollider,
                modifications.thighR2DynamicBoneCollider,
                modifications.stomachDynamicBoneCollider
            };

            Array.Resize(ref characterModel.baseRendererInfos, characterModel.baseRendererInfos.Length + 1);

            var skirtRenderer = modifications.skirtInstance.transform.Find("Skirt").GetComponent<SkinnedMeshRenderer>();
            characterModel.baseRendererInfos[characterModel.baseRendererInfos.Length - 1] = new CharacterModel.RendererInfo
            {
                renderer = skirtRenderer,
                ignoreOverlays = false,
                defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                defaultMaterial = skirtRenderer.sharedMaterial
            };
        }

        private static void ApplySwordAccessoriesModifications(GameObject modelObject, ModificationObjects modifications, CharacterModel characterModel)
        {
            var swordBase = modelObject.transform.Find("MercArmature/ROOT/base/stomach/chest/SwingCenter/SwordBase");

            modifications.swordPointLight = swordBase.Find("Point Light")?.gameObject;
            if (modifications.swordPointLight)
            {
                modifications.swordPointLight.SetActive(false);
            }

            modifications.swordAccessoryInstance = Instantiate(SwordAccessoryPrefab, modelObject.transform, false);

            modifications.swordAccessoryArmature = modifications.swordAccessoryInstance.transform.Find("SwordAccessoryArmature").gameObject;
            modifications.swordAccessoryArmature.transform.SetParent(swordBase, false);

            modifications.swordAccessoryDynamicBone = modelObject.AddComponent<DynamicBone>();
            modifications.swordAccessoryDynamicBone.m_Root = modifications.swordAccessoryArmature.transform.GetChild(0);
            modifications.swordAccessoryDynamicBone.m_Force = new Vector3(0, -0.05F, 0);
            modifications.swordAccessoryDynamicBone.m_Damping = 0;
            modifications.swordAccessoryDynamicBone.m_Elasticity = 0.05F;
            modifications.swordAccessoryDynamicBone.m_Stiffness = 0;
            modifications.swordAccessoryDynamicBone.m_Inert = 0;

            Array.Resize(ref characterModel.baseRendererInfos, characterModel.baseRendererInfos.Length + 1);

            var swordAccessoryRenderer = modifications.swordAccessoryInstance.transform.Find("SwordAccessory").GetComponent<SkinnedMeshRenderer>();
            characterModel.baseRendererInfos[characterModel.baseRendererInfos.Length - 1] = new CharacterModel.RendererInfo
            {
                renderer = swordAccessoryRenderer,
                ignoreOverlays = false,
                defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                defaultMaterial = swordAccessoryRenderer.sharedMaterial
            };
        }
    }
}
