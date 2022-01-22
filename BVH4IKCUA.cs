using System.Collections.Generic;
using UniHumanoid;
using UnityEngine;
using MVR.FileManagementSecure;

class BVH4IKCUA : MVRScript
{
	bool attachedIKCUA = false;
	string IKCUAPluginID = null;
	const string IKCUAName = "IKCUA.VamIK";
	HumanPoseTransfer hpt;
	string prevFolder;
	bool subscene = true;
	Animator anim;
	BvhImporterContext m_context;
	HumanPoseTransfer box;


	public GameObject getActualContainingGOM(bool forceSS = false)
	{
		if (this.containingAtom.isSubSceneRestore || subscene || forceSS)
		{
			return this.containingAtom.containingSubScene.containingAtom.gameObject;
		}
		else
		{
			return containingAtom.gameObject;
		}
	}

	public Atom getActualContainingAtom(bool forceSS = false)
	{
		if (this.containingAtom.isSubSceneRestore || subscene || forceSS)
			return SuperController.singleton.GetAtomByUid(this.containingAtom.subScenePath.Split('/')[0]);
		else
			return containingAtom;
	}

	public JSONStorable GetStorableByPartialID(Atom ato, string st)
	{
		SuperController.LogMessage(st);
		foreach (string ids in ato.GetStorableIDs())
		{

			if (ids != null && ids.ToLower().Contains(st.ToLower()))
			{

				return ato.GetStorableByID(ids);
			}

		}
		return null;
	}

	protected void loadAnimation(string path)
	{
		JSONStorable ikcua = GetStorableByPartialID(this.containingAtom, IKCUAName);

		var bindings = new List<object>();
		if (ikcua != null)
		{

			ikcua.SendMessage("getAnimator", bindings, SendMessageOptions.DontRequireReceiver);

			if (bindings.Count > 0)
			{
				anim = (Animator)bindings[0];

				m_context = anim.gameObject.AddComponent<BvhImporterContext>();
				m_context.Parse(path);

				m_context.Load();

				HumanPoseTransfer hpt = anim.gameObject.AddComponent<HumanPoseTransfer>();

				hpt.SourceType = HumanPoseTransfer.HumanPoseTransferSourceType.HumanPoseTransfer;
				hpt.Avatar = anim.avatar;

				var src = m_context.Root.AddComponent<HumanPoseTransfer>();

				hpt.SourceType = HumanPoseTransfer.HumanPoseTransferSourceType.HumanPoseTransfer;
				hpt.Source = src;



			}

		}
	}

	public override void Init()
	{
		base.Init();


		CreateButton("Select BVH File").button.onClick.AddListener(() =>
		{
			if (prevFolder == "")
				prevFolder = FileManagerSecure.GetDirectoryName("Custom/xps"); ;// SuperController.singleton.dir + "Custom\\XPS";
			SuperController.singleton.GetMediaPathDialog((string path) =>
			{
				if (!path.Equals(""))
				{

					loadAnimation(path);


					prevFolder = FileManagerSecure.GetDirectoryName(path);
				}
				SuperController.singleton.BroadcastMessage("OnActionsProviderAvailable", this, SendMessageOptions.DontRequireReceiver);
			}, "bvh", prevFolder, true);


		});

		CreateButton("delete robot").button.onClick.AddListener(() =>
		{
			Destroy(m_context.Root);
		});

		CreateButton("play").button.onClick.AddListener(() =>
		{
			StartAnimation();
		});

		CreateButton("pause").button.onClick.AddListener(() =>
		{
			StopAnimation();
		});

		CreateButton("reset").button.onClick.AddListener(() =>
		{
			RestartAnimation();
		});

		JSONStorableAction playanimation = new JSONStorableAction("StartAnimation", StartAnimation);
		RegisterAction(playanimation);
		JSONStorableAction rewindanimation = new JSONStorableAction("RestartAnimation", RestartAnimation);
		RegisterAction(rewindanimation);
		JSONStorableAction pauseanimation = new JSONStorableAction("StopAnimation", StopAnimation);
		RegisterAction(pauseanimation);



	}

	public void StartAnimation()
	{
		m_context.srcAnimation.Play();
	}

	public void StopAnimation()
	{
		m_context.srcAnimation.Stop();
	}

	public void RestartAnimation()
	{
		m_context.srcAnimation.Rewind();
	}
}

