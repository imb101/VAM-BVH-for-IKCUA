using System.Collections.Generic;
using UniHumanoid;
using UnityEngine;
using MVR.FileManagementSecure;
using System;
using SimpleJSON;
using System.Collections;
using System.Linq;

class BVH4IKCUA : MVRScript, IDisposable
{
	bool attachedIKCUA = false;
	string IKCUAPluginID = null;
	const string IKCUAName = "IKCUA.VamIK";
	HumanPoseTransfer hpt;
	string prevFolder;
	
	JSONClass pluginJson;
	bool subscene = false;

	Animator anim;
	BvhImporterContext m_context;
	HumanPoseTransfer box;
	GameObject bvhRoot;
	SkinnedMeshRenderer boxMan;
	JSONStorableString bvhPath;
	JSONStorable ikcua;
	JSONStorableBool showSkeleton;
	JSONStorableBool IKFollowAnimation;
	JSONStorableBool AutoPlay;

	JSONStorableFloat frameCounter;
	UIDynamicSlider frameSlider;

	JSONStorableFloat startFrame;
	UIDynamicSlider startFrameSlider;

	JSONStorableFloat endFrame;
	UIDynamicSlider endFrameSlider;

	JSONStorableFloat speed;
	UIDynamicSlider speedSlider;

	JSONStorableFloat frameScrubber;
	UIDynamicSlider frameScrubberSlider;

	JSONStorableStringChooser uiPlayMode;

	bool clipSelected = false;
	bool ikFollowAnimation = true;
	Dictionary<Transform, Atom> effectors;
	Atom headEffector;
	bool animationLoadComplete = false;

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

	protected void loadAnimation(string path, bool restore)
	{
		animationLoadComplete = false;
		ikcua = GetStorableByPartialID(this.containingAtom, IKCUAName);

		var animatorBindings = new List<object>();
		var resetPoseBindings = new List<object>();
		var effectorsBindings = new List<object>();

		resetPoseBindings.Add(false);

		if (ikcua != null)
		{
			ikcua.SendMessage("getAnimator", animatorBindings, SendMessageOptions.RequireReceiver);

			if (animatorBindings.Count > 0)
			{
				anim = (Animator)animatorBindings[0];
		    	//ikcua.SendMessage("resetPose", resetPoseBindings, SendMessageOptions.RequireReceiver); //move to t-pose
				ikcua.SendMessage("getFullBodyEffectors", effectorsBindings, SendMessageOptions.RequireReceiver); //move to t-pose

				effectors = (Dictionary < Transform, Atom > )effectorsBindings[0];
				headEffector = (Atom)effectorsBindings[1];

				m_context = anim.gameObject.AddComponent<BvhImporterContext>();				
				m_context.Parse(path);
				
					bvhRoot = new GameObject("BVH_ROOT");
				
					m_context.Load(bvhRoot,2f);

					HumanPoseTransfer hpt = anim.gameObject.AddComponent<HumanPoseTransfer>();

					var src = m_context.Root.AddComponent<HumanPoseTransfer>();

					hpt.SourceType = HumanPoseTransfer.HumanPoseTransferSourceType.HumanPoseTransfer;
					hpt.controller = this.containingAtom;
					src.controller = this.containingAtom;
					hpt.Source = src;
					
					boxMan = bvhRoot.GetComponentInChildren<SkinnedMeshRenderer>(true);

				if (!showSkeleton.val)
					boxMan.enabled = false;

				bvhRoot.transform.parent = this.containingAtom.freeControllers[0].transform;
				bvhRoot.transform.position = this.containingAtom.freeControllers[0].transform.position;
				bvhRoot.transform.rotation = this.containingAtom.freeControllers[0].transform.rotation;
				frameSlider.slider.interactable = false;

				
				frameCounter.max = m_context.Animation.length;									
				startFrame.max = m_context.Animation.length;
				endFrame.max = m_context.Animation.length;
				endFrame.defaultVal = m_context.Animation.length;
				endFrame.val = m_context.Animation.length;				
				frameScrubber.max = m_context.Animation.length;

				if (restore && pluginJson != null)
				{
					frameCounter.RestoreFromJSON(pluginJson);
					startFrame.RestoreFromJSON(pluginJson);
					endFrame.RestoreFromJSON(pluginJson);
					speed.RestoreFromJSON(pluginJson);
					frameScrubber.RestoreFromJSON(pluginJson);
				}
				speed.setCallbackFunction += delegate (float val) { m_context.srcAnimation[m_context.srcAnimation.name].speed = val; };
				frameScrubber.setCallbackFunction += delegate (float val) { m_context.srcAnimation[m_context.srcAnimation.name].time = val; };
				m_context.srcAnimation[m_context.srcAnimation.name].speed = speed.val;
				animationLoadComplete = true;
			}

		}
		
		clipSelected = true;
	}
	

	public override void Init()
	{
	
		if (this.containingAtom.isSubSceneRestore || containingAtom.name.Contains('/'))
			subscene = true;
		else
			subscene = false;

		checkForOtherPlugins();

		frameCounter = new JSONStorableFloat("Frame", 0, 0, 0);
		RegisterFloat(frameCounter);
		startFrame = new JSONStorableFloat("Start Frame", 0, 0, 0);
		RegisterFloat(startFrame);
		endFrame = new JSONStorableFloat("End Frame", 0, 0, 0);
		RegisterFloat(endFrame);
		speed = new JSONStorableFloat("Play Speed", 1, (float val) => {  }, 0, 5, false);
		RegisterFloat(speed);
		frameScrubber = new JSONStorableFloat("Frame Scrubber", 0, (float val) => {  }, 0, 0);
		RegisterFloat(frameScrubber);

		bvhPath = new JSONStorableString("bvhPath","");
		RegisterString(bvhPath);
		IKFollowAnimation = new JSONStorableBool("ikFollowsAnimation", true, (bool val) => { ikFollowAnimation = IKFollowAnimation.val; });
		AutoPlay= new JSONStorableBool("AutoPlay", true);

		var playModes = new List<string>();
		playModes.Add(WrapMode.Once.ToString());
		playModes.Add(WrapMode.Loop.ToString());
		playModes.Add(WrapMode.PingPong.ToString());

		uiPlayMode = new JSONStorableStringChooser("playmode", playModes, playModes[0], "Play Mode", (string val) => {

			WrapMode wrapMode = (WrapMode)System.Enum.Parse(typeof(WrapMode), val);

			if (clipSelected)
				m_context.srcAnimation.wrapMode = wrapMode;

		});

		CreateButton("Select BVH File").button.onClick.AddListener(() =>
		{
			if (prevFolder == "")
				prevFolder = FileManagerSecure.GetDirectoryName("Custom/bvh"); 
			SuperController.singleton.GetMediaPathDialog((string path) =>
			{
				if (!path.Equals(""))
				{
					bvhPath.val = path;
					clipSelected = false;
					bvhPath.val = path;
					Destroy(bvhRoot);

					loadAnimation(path, false);
					WrapMode wrapMode = (WrapMode)System.Enum.Parse(typeof(WrapMode), uiPlayMode.val);
					m_context.srcAnimation.wrapMode = wrapMode;
					prevFolder = FileManagerSecure.GetDirectoryName(path);

					if (AutoPlay.val)
						StartAnimation();
				}				
			}, "bvh", prevFolder, true);


		});
		RegisterStringChooser(uiPlayMode);

		showSkeleton = new JSONStorableBool("Show Skeleton", false, (bool val) => {
			boxMan.enabled = val;
		});
		RegisterBool(showSkeleton);
		CreateToggle(showSkeleton);

		RegisterBool(IKFollowAnimation);
		CreateToggle(IKFollowAnimation);

		RegisterBool(AutoPlay);
		CreateToggle(AutoPlay);

		CreatePopup(uiPlayMode);

		CreateButton("Play").button.onClick.AddListener(() =>
		{
			StartAnimation();
		});
		CreateButton("Pause Animation").button.onClick.AddListener(() =>
		{
			PauseAnimation();
		});
		CreateButton("Stop Animation").button.onClick.AddListener(() =>
		{
			StopAnimation();
		});
		CreateButton("Reset").button.onClick.AddListener(() =>
		{	
			RestartAnimation();
		});

		frameSlider = CreateSlider(frameCounter, true);
		startFrameSlider = CreateSlider(startFrame, true);
		endFrameSlider = CreateSlider(endFrame, true);
		speedSlider = CreateSlider(speed, true);
		frameScrubberSlider = CreateSlider(frameScrubber, true);

		JSONStorableAction playanimation = new JSONStorableAction("StartAnimation", StartAnimation);
		RegisterAction(playanimation);
		JSONStorableAction rewindanimation = new JSONStorableAction("RestartAnimation", RestartAnimation);
		RegisterAction(rewindanimation);
		JSONStorableAction pauseanimation = new JSONStorableAction("PauseAnimation", StopAnimation);
		RegisterAction(pauseanimation);
	}
	
	protected void UpdateModel()
	{
		if (m_context.srcAnimation.isPlaying && m_context.srcAnimation[m_context.srcAnimation.name].speed != 0f)
		{
			foreach(KeyValuePair<Transform, Atom> effect in effectors)
			{
				effect.Value.freeControllers[0].transform.position = effect.Key.position;
				effect.Value.freeControllers[0].transform.rotation = effect.Key.rotation;
			}

			Transform head = anim.GetBoneTransform(HumanBodyBones.Head);

		//	headEffector.freeControllers[0].transform.position = head.position; 
		//	headEffector.freeControllers[0].transform.rotation = head.rotation;
		}
		
	}

	public void Update()
	{
		if(clipSelected)
		{ 
			if(frameCounter != null && m_context!=null && m_context.srcAnimation.isPlaying)
			{
				frameCounter.val = m_context.srcAnimation[m_context.srcAnimation.name].time;
			}

			if(m_context.srcAnimation[m_context.srcAnimation.name].time >= endFrame.val)
			{
				m_context.srcAnimation[m_context.srcAnimation.name].time = startFrame.val;

				if(m_context.srcAnimation[m_context.srcAnimation.name].wrapMode == WrapMode.Once)
				{
					m_context.srcAnimation.Stop();
				}
			}

			if (startFrame.val!=0 && m_context.srcAnimation.isPlaying)
			{
				if (m_context.srcAnimation[m_context.srcAnimation.name].time < startFrame.val)
					m_context.srcAnimation[m_context.srcAnimation.name].time = startFrame.val+0.00001f;
			}

		
		}
	}

	public void LateUpdate()
	{

		if (animationLoadComplete && clipSelected && ikcua!=null)
		{
			if (m_context != null && ikFollowAnimation && m_context.srcAnimation.isPlaying)
			{
				UpdateModel();
			}
		}
	}

	public void StartAnimation()
	{		
		ikcua.SendMessage("disableFullBodyIK", new List<object>(), SendMessageOptions.RequireReceiver);	
		m_context.srcAnimation.Play();
		m_context.srcAnimation[m_context.srcAnimation.name].speed = speed.val;
	}

	public void PauseAnimation()
	{
		m_context.srcAnimation[m_context.srcAnimation.name].speed = 0f;		
	}

	public void StopAnimation()
	{
		m_context.srcAnimation.Stop();
		m_context.srcAnimation[m_context.srcAnimation.name].speed = speed.val;
		ikcua.SendMessage("enableFullBodyIK", new List<object>(), SendMessageOptions.RequireReceiver);
	}

	public void RestartAnimation()
	{
		m_context.srcAnimation[m_context.srcAnimation.name].time = startFrame.val ;		
	}

	public void Dispose()
	{
		Destroy(bvhRoot);
	}

	private JSONClass extractPluginJSON(JSONNode file, string id)
	{
		JSONClass retJson = null;

		JSONNode sceneFile = file.AsObject["atoms"];

		foreach (JSONNode st in sceneFile.Childs)
		{
			if (st["id"].ToString().Equals("\"" + id + "\""))
			{

				foreach (JSONNode subSt in st["storables"].Childs)
				{
					if (subSt["id"].ToString().Equals("\"" + storeId + "\""))
					{
						retJson = subSt.AsObject;
						break;
					}
				}
				break;
			}
		}

		return retJson;
	}

	public override void PostRestore()
	{
		pluginJson = null;

		if (!subscene)
		{
			pluginJson = extractPluginJSON(SuperController.singleton.loadJson, this.AtomUidToStoreAtomUid(this.containingAtom.uid));
			RestoreFromJSON((JSONClass)pluginJson);
		}
		else
		{

			JSONNode subsceneSave = SuperController.singleton.GetSaveJSON(this.containingAtom.parentAtom).AsObject["atoms"]; ;
			string ssPath = null;

			foreach (JSONNode st in subsceneSave.Childs)
			{
				if (st["id"].ToString().Equals("\"" + this.containingAtom.subScenePath.TrimEnd('/') + "\""))
				{

					foreach (JSONNode subSt in st["storables"].Childs)
					{

						if (subSt["id"].ToString().Equals("\"" + this.containingAtom.containingSubScene.storeId + "\""))
						{
							pluginJson = subSt.AsObject;
							ssPath = subSt["storePath"];

							break;
						}
					}
					break;
				}
			}

			//if ss path!=null and it doesn't contain a / it means its just been made.. have to goto the UI to get where.

			if (ssPath != null && !ssPath.Contains("/"))
			{
				SubScene subSceneComp = this.containingAtom.containingSubScene;
				SubSceneUI subSceneUI = subSceneComp.UITransform.GetComponentInChildren<SubSceneUI>();
				ssPath = "Custom/SubScene/" + subSceneUI.creatorNameInputField.text + "/" + subSceneUI.signatureInputField.text + "/" + ssPath;
			}

			if (ssPath != null && ssPath.Contains("/"))
			{
				try
				{
					JSONNode subsceneNode = SuperController.singleton.LoadJSON(ssPath);
					pluginJson = extractPluginJSON(subsceneNode, this.AtomUidToStoreAtomUid(this.containingAtom.uid).Split('/')[1]);
				}
				catch (Exception e)
				{
					SuperController.LogMessage("Unable to load stored JSON: " + ssPath);
				}

				if (pluginJson != null)
					RestoreFromJSON((JSONClass)pluginJson);
			}

		}

		base.PostRestore();

		StartCoroutine(restoreAnimation());
	}

	private void checkForOtherPlugins()
	{
		foreach (string st in this.containingAtom.GetStorableIDs())
		{
			if (st.Contains(IKCUAName))
			{
				IKCUAPluginID = st;
				attachedIKCUA = true;
			}
		}

	}

	private IEnumerator restoreAnimation()
	{
		//do we have an XPS loader ?
		checkForOtherPlugins();
		
		while (SuperController.singleton.isLoading)
		{
			yield return null;
		}

		if (attachedIKCUA)
		{
			ikcua = GetStorableByPartialID(this.containingAtom, IKCUAName);
			var bindings = new List<object>();
			if (ikcua != null) //find FBB loading!
			{
				ikcua.SendMessage("isFullBodyIKAttached", bindings, SendMessageOptions.DontRequireReceiver);
				bool modlLoad = (bool)bindings[0];
				while (!modlLoad)
				{
					ikcua.SendMessage("isFullBodyIKAttached", bindings, SendMessageOptions.RequireReceiver);
					modlLoad = (bool)bindings[0];
					yield return null;
				}
			}
		}

		loadAnimation(bvhPath.val, true);
		
		WrapMode wrapMode = (WrapMode)System.Enum.Parse(typeof(WrapMode), uiPlayMode.val);
		m_context.srcAnimation.wrapMode = wrapMode;
		
		if (AutoPlay.val)
			StartAnimation();
	}
}

