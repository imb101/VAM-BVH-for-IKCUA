using UnityEngine;


namespace UniHumanoid
{
    public class HumanPoseTransfer : MonoBehaviour
    {
        public class HumanPoseTransferSourceType
        {
            public const int None = 0;
            public const int HumanPoseTransfer = 1;
            public const int HumanPoseClip = 2;
        }

        public int SourceType;

        public Avatar Avatar;
        public Atom controller;

        #region Standalone
        public HumanPose CreatePose()
        {
            
            var handler = new HumanPoseHandler(Avatar, transform);
            var pose = default(HumanPose);

            handler.GetHumanPose(ref pose);

            return pose;
        }
        public void SetPose(HumanPose pose)
        {
            SetPose(Avatar, transform, pose);
        }
        public static void SetPose(Avatar avatar, Transform transform, HumanPose pose)
        {
            var handler = new HumanPoseHandler(avatar, transform);
            handler.SetHumanPose(ref pose);
        }

        public static void SetTPose(Avatar avatar, Transform transform)
        {
            var humanPoseClip = Resources.Load<HumanPoseClip>(HumanPoseClip.TPoseResourcePath);
            var pose = humanPoseClip.GetPose();
            HumanPoseTransfer.SetPose(avatar, transform, pose);
        }
        #endregion

        private void Reset()
        {
            var animator = GetComponent<Animator>();
            if (animator != null)
            {
                Avatar = animator.avatar;
            }
        }


        public HumanPoseTransfer Source;
        HumanPoseHandler m_handler;

        public HumanPoseClip PoseClip;


        public void SetTPose()
        {
            if (Avatar == null) return;
            SetTPose(Avatar, transform);
        }

       
        public void OnEnable()
        {
            var animator = GetComponent<Animator>();
            if (animator != null)
            {
                Avatar = animator.avatar;
            }

            Setup();
        }

        public void Setup()
        {
            if (Avatar == null)
            {
                return;
            }
            
            m_handler = new HumanPoseHandler(Avatar, transform);
        }

        HumanPose m_pose;

        int m_lastFrameCount = -1;

        public bool GetPose(int frameCount, ref HumanPose pose)
        {            
            if (PoseClip != null)
            {
                pose = PoseClip.GetPose();
              //  pose.bodyPosition = controller.freeControllers[0].transform.position;
            //    pose.bodyRotation = controller.freeControllers[0].transform.rotation;
                return true;
            }

            if (m_handler == null)
            {
                pose = m_pose;
              //  m_pose.bodyPosition = controller.freeControllers[0].transform.position;
             //   m_pose.bodyRotation = controller.freeControllers[0].transform.rotation;
                return false;
            }

            if (frameCount != m_lastFrameCount)
            {
                m_handler.GetHumanPose(ref m_pose);
                //m_pose.bodyPosition = controller.freeControllers[0].transform.position;
                //m_pose.bodyRotation = controller.freeControllers[0].transform.rotation;
                m_lastFrameCount = frameCount;
            }
           // m_pose.bodyPosition = controller.freeControllers[0].transform.position;
           // m_pose.bodyRotation = controller.freeControllers[0].transform.rotation;
            pose = m_pose;
                    
            return true;
        }


        public void Update()
        {
            switch (SourceType)
            {
                case HumanPoseTransferSourceType.None:
                    break;

                case HumanPoseTransferSourceType.HumanPoseTransfer:                                           
                    if (Source != null && m_handler != null)
                    {

                        if (Source.GetPose(Time.frameCount, ref m_pose))
                        {                                                        
                            m_handler.SetHumanPose(ref m_pose);
                        }
                    }
                    break;

                case HumanPoseTransferSourceType.HumanPoseClip:
                    if (PoseClip != null)
                    {
                        var pose = PoseClip.GetPose();                      
                        m_handler.SetHumanPose(ref pose);
                    }
                    break;
            }
        }
    }
}
