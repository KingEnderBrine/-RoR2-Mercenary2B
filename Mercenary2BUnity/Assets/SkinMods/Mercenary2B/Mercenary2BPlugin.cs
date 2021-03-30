using BepInEx;
using BepInEx.Logging;
using RoR2;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using System.Security;
using System.Security.Permissions;
using MonoMod.RuntimeDetour;

#pragma warning disable CS0618 // Type or member is obsolete
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[assembly: R2API.Utils.ManualNetworkRegistration]
[assembly: EnigmaticThunder.Util.ManualNetworkRegistration]
namespace Mercenary2B
{
    
    [BepInPlugin("com.KingEnderBrine.Mercenary2B","Mercenary2B","1.2.0")]
    public partial class Mercenary2BPlugin : BaseUnityPlugin
    {
        internal static Mercenary2BPlugin Instance { get; private set; }
        internal static ManualLogSource InstanceLogger { get; private set; }
        
        private static AssetBundle assetBundle;
        private static readonly List<Material> materialsWithRoRShader = new List<Material>();
        private void Awake()
        {
            Instance = this;
            BeforeAwake();
            using (var assetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Mercenary2B.kingenderbrinemercenary2b"))
            {
                assetBundle = AssetBundle.LoadFromStream(assetStream);
            }

            BodyCatalog.availability.CallWhenAvailable(BodyCatalogInit);
            new Hook(typeof(Language).GetMethod(nameof(Language.LoadStrings)), (Action<Action<Language>, Language>)LanguageLoadStrings).Apply();

            ReplaceShaders();

            AfterAwake();
        }

        partial void BeforeAwake();
        partial void AfterAwake();
        static partial void BeforeBodyCatalogInit();
        static partial void AfterBodyCatalogInit();

        private static void ReplaceShaders()
        {
            materialsWithRoRShader.Add(LoadMaterialWithReplacedShader(@"Assets/Mercenary2B/Resources/Materials/mat2B.mat", @"Hopoo Games/Deferred/Standard"));
            materialsWithRoRShader.Add(LoadMaterialWithReplacedShader(@"Assets/Mercenary2B/Resources/Materials/matSword.mat", @"Hopoo Games/Deferred/Standard"));
        }

        private static Material LoadMaterialWithReplacedShader(string materialPath, string shaderName)
        {
            var material = assetBundle.LoadAsset<Material>(materialPath);
            material.shader = Shader.Find(shaderName);

            return material;
        }

        private static void LanguageLoadStrings(Action<Language> orig, Language self)
        {
            orig(self);

            switch(self.name.ToLower())
            {
                case "en":
                    self.SetStringByToken("KINGENDERBRINE_SKIN_MERCENARY2B_NAME", "2B");
                    break;
                default:
                    self.SetStringByToken("KINGENDERBRINE_SKIN_MERCENARY2B_NAME", "2B");
                    break;
            }
        }

        private static void Nothing(Action<SkinDef> orig, SkinDef self)
        {

        }

        private static void BodyCatalogInit()
        {
            BeforeBodyCatalogInit();

            var hook = new Hook(typeof(SkinDef).GetMethod(nameof(SkinDef.Awake), BindingFlags.NonPublic | BindingFlags.Instance), (Action<Action<SkinDef>, SkinDef>)Nothing);
            hook.Apply();

            AddMercBodyMercenary2BSkin();

            hook.Undo();

            AfterBodyCatalogInit();
        }

        static partial void MercBodyMercenary2BSkinAdded(SkinDef skinDef, GameObject bodyPrefab);

        private static void AddMercBodyMercenary2BSkin()
        {
            var bodyName = "MercBody";
            var skinName = "Mercenary2B";
            try
            {
                var bodyPrefab = BodyCatalog.FindBodyPrefab(bodyName);
                var modelLocator = bodyPrefab.GetComponent<ModelLocator>();
                var mdl = modelLocator.modelTransform.gameObject;
                var skinController = mdl.GetComponent<ModelSkinController>();

                var renderers = mdl.GetComponentsInChildren<Renderer>(true);

                var skin = ScriptableObject.CreateInstance<SkinDef>();
                skin.icon = assetBundle.LoadAsset<Sprite>(@"Assets\SkinMods\Mercenary2B\Icons\Mercenary2BIcon.png");
                skin.name = skinName;
                skin.nameToken = "KINGENDERBRINE_SKIN_MERCENARY2B_NAME";
                skin.rootObject = mdl;
                skin.baseSkins = Array.Empty<SkinDef>();
                skin.unlockableDef = null;
                skin.gameObjectActivations = Array.Empty<SkinDef.GameObjectActivation>();
                skin.rendererInfos = new CharacterModel.RendererInfo[]
                {
                    new CharacterModel.RendererInfo
                    {
                        defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Mercenary2B/Resources/Materials/mat2B.mat"),
                        defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                        ignoreOverlays = false,
                        renderer = renderers[3]
                    },
                    new CharacterModel.RendererInfo
                    {
                        defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Mercenary2B/Resources/Materials/matSword.mat"),
                        defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                        ignoreOverlays = false,
                        renderer = renderers[4]
                    },
                };
                skin.meshReplacements = new SkinDef.MeshReplacement[]
                {
                    new SkinDef.MeshReplacement
                    {
                        mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Mercenary2B\Meshes\Nier2b.mesh"),
                        renderer = renderers[3]
                    },
                    new SkinDef.MeshReplacement
                    {
                        mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\Mercenary2B\Meshes\Sword.mesh"),
                        renderer = renderers[4]
                    },
                };
                skin.minionSkinReplacements = Array.Empty<SkinDef.MinionSkinReplacement>();
                skin.projectileGhostReplacements = Array.Empty<SkinDef.ProjectileGhostReplacement>();

                Array.Resize(ref skinController.skins, skinController.skins.Length + 1);
                skinController.skins[skinController.skins.Length - 1] = skin;

                BodyCatalog.skins[(int)BodyCatalog.FindBodyIndex(bodyPrefab)] = skinController.skins;
                MercBodyMercenary2BSkinAdded(skin, bodyPrefab);
            }
            catch (Exception e)
            {
                InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\"");
                InstanceLogger.LogError(e);
            }
        }
    }

}

namespace R2API.Utils
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ManualNetworkRegistrationAttribute : Attribute { }
}

namespace EnigmaticThunder.Util
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ManualNetworkRegistrationAttribute : Attribute { }
}