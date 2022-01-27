using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using MVR.FileManagementSecure;



namespace UniHumanoid
{

    public class BvhImporterContext : MonoBehaviour
    {
        #region Source
        String m_path;
        public String Path
        {
            get { return m_path; }
            set
            {
                if (m_path == value) return;
                m_path = value;
            }
        }
        public String Source; // source
        public Bvh Bvh;
        #endregion

        #region Imported
        public GameObject Root;
        public List<Transform> Nodes = new List<Transform>();
        public AnimationClip Animation;
        public Animation srcAnimation;
        public AvatarDescription AvatarDescription;
        public Avatar Avatar;
        public Mesh Mesh;
        public Material Material;
        #endregion

        #region Load
        [Obsolete("use Load(path)")]
        public void Parse()
        {
            Parse(Path);
        }

        public void Parse(string path)
        {
            Path = path;
            Source = FileManagerSecure.ReadAllText(Path);//, Encoding.UTF8);
            Bvh = Bvh.Parse(Source);
        }

        public void Load(float height)
        {
            var Root = new GameObject(FileManagerSecure.GetFileName(Path));
            Load(Root, height);
        }

        public void Load(GameObject Root_, float height)
        {
            //
            // build hierarchy
            //
            Root = Root_;
            var hips = BuildHierarchy(Root.transform, Bvh.Root,height);
            var skeleton = Skeleton.Estimate(hips);
            var description = AvatarDescription.Create(hips.Traverse().ToArray(), skeleton);

            //
            // scaling. reposition
            //
           float scaling = 1.0f;
           {
                       //var foot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                       var foot = hips.Traverse().Skip(skeleton.GetBoneIndex(HumanBodyBones.LeftFoot)).First();
                       var hipHeight = hips.position.y - foot.position.y;
                       // hips height to a meter
                       scaling = 1.0f / hipHeight;
                       foreach (var x in Root.transform.Traverse())
                       {
                           x.localPosition *= scaling;
                       }
                       //+scaledHeight
                       var scaledHeight = hipHeight * scaling;
                      // hips.localPosition = new Vector3(scaledHeightRoot_.transform.position.x , Root_.transform.position.y * scaledHeight, Root_.transform.position.z * scaledHeight); // foot to ground
                      // hips.position = new Vector3(0,  scaledHeight, 0); // foot to ground
                       hips.position = new Vector3(Root_.transform.position.x, scaledHeight, Root_.transform.position.z); // foot to ground
                       //hips.position = new Vector3(Root_.transform.position.x, Root_.transform.position.y+scaledHeight, Root_.transform.position.z); // foot to ground
            }

            //
            // avatar
            //
            Avatar = description.CreateAvatar(Root.transform);
            Avatar.name = "Avatar";
            AvatarDescription = description;
            var animator = Root.AddComponent<Animator>();
            animator.avatar = Avatar;

            //
            // create AnimationClip
            //
            Animation = BvhAnimation.CreateAnimationClip(Bvh, scaling);
            Animation.name = Root.name;
            Animation.legacy = true;
            Animation.wrapMode = WrapMode.Once;

            srcAnimation = Root.AddComponent<Animation>();
            srcAnimation.AddClip(Animation, Animation.name);
            srcAnimation.clip = Animation;            

            var humanPoseTransfer = Root.AddComponent<HumanPoseTransfer>();
            humanPoseTransfer.Avatar = Avatar;

            // create SkinnedMesh for bone visualize
            var renderer = SkeletonMeshUtility.CreateRenderer(animator);
            Material = new Material(Shader.Find("Standard"));
            renderer.sharedMaterial = Material;
            Mesh = renderer.sharedMesh;
            Mesh.name = "box-man";

            Root.AddComponent<BoneMapping>();

        }

        static Transform BuildHierarchy(Transform parent, BvhNode node, float toMeter)
        {
            var go = new GameObject(node.Name);

            go.transform.position = node.Offset.ToXReversedVector3() * toMeter;
            go.transform.SetParent(parent, false);
           
            //var gizmo = go.AddComponent<BoneGizmoDrawer>();
            //gizmo.Draw = true;

            foreach (var child in node.Children)
            {
                BuildHierarchy(go.transform, child, toMeter);
            }

            return go.transform;
        }
        #endregion


        public void Destroy(bool destroySubAssets)
        {
            if (Root != null) GameObject.DestroyImmediate(Root);
            if (destroySubAssets)
            {

            }
        }
    }
}
