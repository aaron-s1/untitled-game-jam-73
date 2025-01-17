﻿// Copyright 2021, Infima Games. All Rights Reserved.

using System;
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

using InfimaGames.LowPolyShooterPack.Interface;

namespace InfimaGames.LowPolyShooterPack
{
	/// <summary>
	/// Main Character Component. This component handles the most important functions of the character, and interfaces
	/// with basically every part of the asset, it is the hub where it all converges.
	/// </summary>
	[RequireComponent(typeof(CharacterKinematics))]
	public sealed class Character : CharacterBehaviour
	{
		#region FIELDS SERIALIZED

		[Header("Inventory")]
		
		[Tooltip("Inventory.")]
		[SerializeField]
		private InventoryBehaviour inventory;

		[Header("Cameras")]

		[Tooltip("Normal Camera.")]
		[SerializeField]
		private Camera cameraWorld;

		[Header("Animation")]

		[Tooltip("Determines how smooth the locomotion blendspace is.")]
		[SerializeField]
		private float dampTimeLocomotion = 0.15f;

		[Tooltip("How smoothly we play aiming transitions. Beware that this affects lots of things!")]
		[SerializeField]
		private float dampTimeAiming = 0.3f;
		
		[Header("Animation Procedural")]
		
		[Tooltip("Character Animator.")]
		[SerializeField]
		private Animator characterAnimator;

		#endregion

		#region FIELDS

		/// <summary>
		/// True if the character is aiming.
		/// </summary>
		private bool aiming;
		/// <summary>
		/// True if the character is running.
		/// </summary>
		private bool running;
		/// <summary>
		/// True if the character has its weapon holstered.
		/// </summary>
		private bool holstered;

		private bool jumping;

		public float jumpForce = 1f;
		public float gravityMultiplier = 1f;
 		public float fallMultiplier = 1f;
		public float jumpDamping = 1.5f; // Damping factor to control the jump acceleration
		public float maxJumpHeight = 4f; // Desired max jump height

		private bool isGrounded;
		private Rigidbody rigidbody;
		
		/// <summary>
		/// Last Time.time at which we shot.
		/// </summary>
		private float lastShotTime;
		
		/// <summary>
		/// Overlay Layer Index. Useful for playing things like firing animations.
		/// </summary>
		private int layerOverlay;
		/// <summary>
		/// Holster Layer Index. Used to play holster animations.
		/// </summary>
		private int layerHolster;
		/// <summary>
		/// Actions Layer Index. Used to play actions like reloading.
		/// </summary>
		private int layerActions;

		float jumpDuration;

		/// <summary>
		/// Character Kinematics. Handles all the IK stuff.
		/// </summary>
		private CharacterKinematics characterKinematics;
		
		/// <summary>
		/// The currently equipped weapon.
		/// </summary>
		private WeaponBehaviour equippedWeapon;
		/// <summary>
		/// The equipped weapon's attachment manager.
		/// </summary>
		private WeaponAttachmentManagerBehaviour weaponAttachmentManager;
		
		/// <summary>
		/// The scope equipped on the character's weapon.
		/// </summary>
		private ScopeBehaviour equippedWeaponScope;
		/// <summary>
		/// The magazine equipped on the character's weapon.
		/// </summary>
		private MagazineBehaviour equippedWeaponMagazine;
		
		/// <summary>
		/// True if the character is reloading.
		/// </summary>
		private bool reloading;
		
		/// <summary>
		/// True if the character is inspecting its weapon.
		/// </summary>
		private bool inspecting;

		/// <summary>
		/// True if the character is in the middle of holstering a weapon.
		/// </summary>
		private bool holstering;

		/// <summary>
		/// Look Axis Values.
		/// </summary>
		private Vector2 axisLook;
		/// <summary>
		/// Look Axis Values.
		/// </summary>
		private Vector2 axisMovement;

		private bool isJumping;
		
		/// <summary>
		/// True if the player is holding the aiming button.
		/// </summary>
		private bool holdingButtonAim;
		/// <summary>
		/// True if the player is holding the running button.
		/// </summary>
		private bool holdingButtonRun;
		/// <summary>
		/// True if the player is holding the firing button.
		/// </summary>
		private bool holdingButtonFire;

		/// <summary>
		/// If true, the tutorial text should be visible on screen.
		/// </summary>
		private bool tutorialTextVisible;

		/// <summary>
		/// True if the game cursor is locked! Used when pressing "Escape" to allow developers to more easily access the editor.
		/// </summary>
		private bool cursorLocked;

		#endregion

		#region CONSTANTS

		/// <summary>
		/// Aiming Alpha Value.
		/// </summary>
		private static readonly int HashAimingAlpha = Animator.StringToHash("Aiming");

		/// <summary>
		/// Hashed "Movement".
		/// </summary>
		private static readonly int HashMovement = Animator.StringToHash("Movement");

		#endregion

		#region UNITY

		protected override void Awake()
		{
			#region Lock Cursor

			//Always make sure that our cursor is locked when the game starts!
			cursorLocked = true;
			//Update the cursor's state.
			UpdateCursorState();

			#endregion

			//Cache the CharacterKinematics component.
			characterKinematics = GetComponent<CharacterKinematics>();

			//Initialize Inventory.
			inventory.Init();

			//Refresh!
			RefreshWeaponSetup();
		}
		protected override void Start()
		{
			//Cache a reference to the holster layer's index.
			layerHolster = characterAnimator.GetLayerIndex("Layer Holster");
			//Cache a reference to the action layer's index.
			layerActions = characterAnimator.GetLayerIndex("Layer Actions");
			//Cache a reference to the overlay layer's index.
			layerOverlay = characterAnimator.GetLayerIndex("Layer Overlay");

			rigidbody = GetComponent<Rigidbody>();

			StartCoroutine(BeatMatch__TestingRepeating());
		}

		float beatLength;
		float beatMatchReloadMultiplier = 1f;

		
		
	public Animator beatChevronsAnim;
	bool beatMatchIsRunning;



    IEnumerator BeatMatch__TestingRepeating()
    {
		yield return new WaitForSeconds(0.1f);

        beatMatchIsRunning = true;
		beatChevronsAnim = GameObject.Find("Beat Chevrons").GetComponent<Animator>();

		AnimatorStateInfo stateInfo = beatChevronsAnim.GetCurrentAnimatorStateInfo(0);

		beatLength = stateInfo.length;

        if (beatLength == 0)
        {
            // yield return new WaitForSeconds(0.5f); // Small delay to ensure Animator is initialized

            // // Wait until the Animator starts playing a state
            // while (!beatChevronsAnim.isInitialized)
            // {
            //     Debug.Log("Waiting for Animator to initialize...");
            //     yield return null;
            // }

            // Fetch the current state information
				// AnimatorStateInfo stateInfo = beatChevronsAnim.GetCurrentAnimatorStateInfo(0);
            // Debug.Log($"Current State: {stateInfo.fullPathHash}, Expected State Hash: {Animator.StringToHash("Base Layer.Chevrons Come In")}");

            // Wait until the Animator is playing the expected state
            // while (!stateInfo.IsName("Base Layer.Chevrons Come In"))
            // {
            //     // Debug.Log("Waiting for Animator to play 'Base Layer.Chevrons Come In' state...");
            //     yield return null; // Wait for the next frame
            //     	// stateInfo = beatChevronsAnim.GetCurrentAnimatorStateInfo(0);
            // }

            // beatLength = stateInfo.length;
        }

        // Start timer and process beat match
        float timeElapsed = 0;


		
        while (timeElapsed < beatLength)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame && !equippedWeapon.HasAmmunition())
            {
                if (timeElapsed >= (beatLength * 0.85f))
                    beatMatchReloadMultiplier = 2f;
                else if (timeElapsed >= (beatLength * 0.6f))
                    beatMatchReloadMultiplier = 1.5f;
                else
                    beatMatchReloadMultiplier = 1;

				// Forcefully end while loop's execution.
				timeElapsed += 2f;

				Debug.Log("beatMatchReloadMultiplier = " + beatMatchReloadMultiplier);
				OnTryPlayReloadForced(beatMatchReloadMultiplier);
            }

            yield return null;
            timeElapsed += Time.deltaTime;
        }


        beatMatchIsRunning = false;

		// Small buffer before starting next beat in case reload occurred
		yield return new WaitForSeconds(0.1f);
        StartCoroutine(BeatMatch__TestingRepeating());
    }


		protected override void Update()
		{
			aiming = holdingButtonAim && CanAim();
			running = holdingButtonRun && CanRun();
			// jumping = !CanPlayJumping();

			Debug.Log("Update(). jumping = " + jumping);


			//Holding the firing button.
			if (holdingButtonFire)
			{
				//Check.
				if (CanPlayAnimationFire() && equippedWeapon.HasAmmunition() && equippedWeapon.IsAutomatic())
				{
					//Has fire rate passed.
					if (Time.time - lastShotTime > 60.0f / equippedWeapon.GetRateOfFire())
						Fire();
				}
			}

			// if (jumping)
			// {
			// 	rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
			// }

			// else if (Keyboard.current.oKey.wasPressedThisFrame)			
				// OnTryPlayReloadForced(beatMatchReloadMultiplier);
				// PlayReloadAnimation();

			//Update Animator.
			UpdateAnimator();
		}

		void FixedUpdate() 
		{
			ApplyCustomGravity();
		}

		void ApplyCustomGravity()
		{
            if (!isGrounded && isJumping)
				rigidbody.velocity = new Vector3(rigidbody.velocity.x, jumpForce, rigidbody.velocity.z);

			if (rigidbody.velocity.y > 0)
				rigidbody.velocity += Vector3.up * Physics.gravity.y * (jumpDamping - 1) * Time.deltaTime;
			else if (rigidbody.velocity.y < 0)
				rigidbody.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
			else 
				rigidbody.velocity += Vector3.up * Physics.gravity.y * (gravityMultiplier - 1) * Time.deltaTime;


            // {
            //     Vector3 gravity = Vector3.down * gravityMultiplier;

            //     if (rigidbody.velocity.y < 0)
            //         gravity *= fallMultiplier;

            //     rigidbody.AddForce(gravity, ForceMode.Acceleration);
            // }
		}

		protected override void LateUpdate()
		{
			//We need a weapon for this!
			if (equippedWeapon == null)
				return;

			//Weapons without a scope should not be a thing! Ironsights are a scope too!
			if (equippedWeaponScope == null)
				return;
			
			//Make sure that we have a kinematics component!
			if (characterKinematics != null)
			{
				//Compute.
				characterKinematics.Compute();
			}
		}
		
		#endregion

		#region GETTERS

		public override Camera GetCameraWorld() => cameraWorld;

		public override InventoryBehaviour GetInventory() => inventory;
		
		public override bool IsCrosshairVisible() => !aiming && !holstered;
		public override bool IsRunning() => running;
		public override bool IsJumping() => jumping;
		
		public override bool IsAiming() => aiming;
		public override bool IsCursorLocked() => cursorLocked;
		
		public override bool IsTutorialTextVisible() => tutorialTextVisible;
		
		public override Vector2 GetInputMovement() => axisMovement;
		public override Vector2 GetInputLook() => axisLook;

		#endregion

		#region METHODS

		/// <summary>
		/// Updates all the animator properties for this frame.
		/// </summary>
		private void UpdateAnimator()
		{
			//Movement Value. This value affects absolute movement. Aiming movement uses this, as opposed to per-axis movement.
			characterAnimator.SetFloat(HashMovement, Mathf.Clamp01(Mathf.Abs(axisMovement.x) + Mathf.Abs(axisMovement.y)), dampTimeLocomotion, Time.deltaTime);
			
			//Update the aiming value, but use interpolation. This makes sure that things like firing can transition properly.
			characterAnimator.SetFloat(HashAimingAlpha, Convert.ToSingle(aiming), 0.25f / 1.0f * dampTimeAiming, Time.deltaTime);

			//Update Animator Aiming.
			const string boolNameAim = "Aim";
			characterAnimator.SetBool(boolNameAim, aiming);
			
			//Update Animator Running.
			const string boolNameRun = "Running";
			characterAnimator.SetBool(boolNameRun, running);

			const string boolNameJump = "Jumping";
			// characterAnimator.SetBool(boolNameJump, jumping);
			characterAnimator.SetBool(boolNameJump, jumping);
		}
		
		/// <summary>
		/// Plays the inspect animation.
		/// </summary>
		private void Inspect()
		{
			//State.
			inspecting = true;
			//Play.
			characterAnimator.CrossFade("Inspect", 0.0f, layerActions, 0);
		}

		// private void Jump()
		// {
		// 	Debug.Log("Jump() set jumping to false");			
		// }
		
		/// <summary>
		/// Fires the character's weapon.
		/// </summary>
		private void Fire()
		{
			//Save the shot time, so we can calculate the fire rate correctly.
			lastShotTime = Time.time;
			//Fire the weapon! Make sure that we also pass the scope's spread multiplier if we're aiming.
			equippedWeapon.Fire();

			//Play firing animation.
			const string stateName = "Fire";
			characterAnimator.CrossFade(stateName, 0.05f, layerOverlay, 0);
		}


		private void PlayReloadAnimation(float multiplier = 1f)
		{
			#region Animation

			//Get the name of the animation state to play, which depends on weapon settings, and ammunition!
			string stateName = equippedWeapon.HasAmmunition() ? "Reload" : "Reload Empty";
			
			// Set reload speed according to how well Hellsinger's beat was matched.
			characterAnimator.SetFloat("ReloadSpeedMultiplier", multiplier);

			characterAnimator.Play(stateName, layerActions, 0.0f);
			reloading = true;

			#endregion

			equippedWeapon.Reload();
		}

		/// <summary>
		/// Equip Weapon Coroutine.
		/// </summary>
		private IEnumerator Equip(int index = 0)
		{
			//Only if we're not holstered, holster. If we are already, we don't need to wait.
			if(!holstered)
			{
				//Holster.
				SetHolstered(holstering = true);
				//Wait.
				yield return new WaitUntil(() => holstering == false);
			}
			//Unholster. We do this just in case we were holstered.
			SetHolstered(false);
			//Play Unholster Animation.
			characterAnimator.Play("Unholster", layerHolster, 0);
			
			//Equip The New Weapon.
			inventory.Equip(index);
			//Refresh.
			RefreshWeaponSetup();
		}

		/// <summary>
		/// Refresh all weapon things to make sure we're all set up!
		/// </summary>
		private void RefreshWeaponSetup()
		{
			//Make sure we have a weapon. We don't want errors!
			if ((equippedWeapon = inventory.GetEquipped()) == null)
				return;
			
			//Update Animator Controller. We do this to update all animations to a specific weapon's set.
			characterAnimator.runtimeAnimatorController = equippedWeapon.GetAnimatorController();

			//Get the attachment manager so we can use it to get all the attachments!
			weaponAttachmentManager = equippedWeapon.GetAttachmentManager();
			if (weaponAttachmentManager == null) 
				return;
			
			//Get equipped scope. We need this one for its settings!
			equippedWeaponScope = weaponAttachmentManager.GetEquippedScope();
			//Get equipped magazine. We need this one for its settings!
			equippedWeaponMagazine = weaponAttachmentManager.GetEquippedMagazine();
		}

		void OnCollisionEnter(Collision collision)
		{
			if (collision.gameObject.CompareTag("Ground"))
			{
				jumping = false;
				isGrounded = true;
				Debug.Log("touching ground");
			}
		}

		private void FireEmpty()
		{
			/*
			 * Save Time. Even though we're not actually firing, we still need this for the fire rate between
			 * empty shots.
			 */
			lastShotTime = Time.time;
			//Play.
			characterAnimator.CrossFade("Fire Empty", 0.05f, layerOverlay, 0);
		}

		/// <summary>
		/// Updates the cursor state based on the value of the cursorLocked variable.
		/// </summary>
		private void UpdateCursorState()
		{
			//Update cursor visibility.
			Cursor.visible = !cursorLocked;
			//Update cursor lock state.
			Cursor.lockState = cursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
		}

		/// <summary>
		/// Updates the "Holstered" variable, along with the Character's Animator value.
		/// </summary>
		private void SetHolstered(bool value = true)
		{
			//Update value.
			holstered = value;
			
			//Update Animator.
			const string boolName = "Holstered";
			characterAnimator.SetBool(boolName, holstered);	
		}
		
		#region ACTION CHECKS

		/// <summary>
		/// Can Fire.
		/// </summary>
		private bool CanPlayAnimationFire()
		{
			//Block.
			if (holstered || holstering)
				return false;

			//Block.
			if (reloading)
				return false;

			//Block.
			if (inspecting)
				return false;

			//Return.
			return true;
		}

		/// <summary>
		/// Determines if we can play the reload animation.
		/// </summary>
		private bool CanPlayAnimationReload()
		{
			//No reloading!
			if (reloading)
				return false;

			//Block while inspecting.
			if (inspecting)
				return false;
			
			//Return.
			return true;
		}

		/// <summary>
		/// Returns true if the character is able to holster their weapon.
		/// </summary>
		/// <returns></returns>
		private bool CanPlayAnimationHolster()
		{
			//Block.
			if (reloading)
				return false;

			//Block.
			if (inspecting)
				return false;
			
			//Return.
			return true;
		}

		private bool CanPlayJumping()
		{
			if (aiming || reloading)
				return false;
			if (inspecting)
				return false;
			if (holdingButtonFire && equippedWeapon.HasAmmunition())
				return false;
			if (jumping || !isGrounded)
				return false;

			return true;
		}

		/// <summary>
		/// Returns true if the Character can change their Weapon.
		/// </summary>
		/// <returns></returns>
		private bool CanChangeWeapon()
		{
			//Block.
			if (holstering)
				return false;

			//Block.
			if (reloading)
				return false;

			//Block.
			if (inspecting)
				return false;
			
			//Return.
			return true;
		}

		/// <summary>
		/// Returns true if the Character can play the Inspect animation.
		/// </summary>
		private bool CanPlayAnimationInspect()
		{
			//Block.
			if (holstered || holstering)
				return false;

			//Block.
			if (reloading)
				return false;

			//Block.
			if (inspecting)
				return false;
			
			//Return.
			return true;
		}

		/// <summary>
		/// Returns true if the Character can Aim.
		/// </summary>
		/// <returns></returns>
		private bool CanAim()
		{
			//Block.
			if (holstered || inspecting)
				return false;

			//Block.
			if (reloading || holstering)
				return false;
			
			//Return.
			return true;
		}
		
		/// <summary>
		/// Returns true if the character can run.
		/// </summary>
		/// <returns></returns>
		private bool CanRun()
		{
			//Block.
			if (inspecting)
				return false;

			//Block.
			if (reloading || aiming)
				return false;

			//While trying to fire, we don't want to run. We do this just in case we do fire.
			if (holdingButtonFire && equippedWeapon.HasAmmunition())
				return false;

			//This blocks running backwards, or while fully moving sideways.
			if (axisMovement.y <= 0 || Math.Abs(Mathf.Abs(axisMovement.x) - 1) < 0.01f)
				return false;
			
			//Return.
			return true;
		}



		#endregion

		#region INPUT

		/// <summary>
		/// Fire.
		/// </summary>
		public void OnTryFire(InputAction.CallbackContext context)
		{
			//Block while the cursor is unlocked.
			if (!cursorLocked)
				return;

			//Switch.
			switch (context)
			{
				//Started.
				case {phase: InputActionPhase.Started}:
					//Hold.
					holdingButtonFire = true;
					break;
				//Performed.
				case {phase: InputActionPhase.Performed}:
					//Ignore if we're not allowed to actually fire.
					if (!CanPlayAnimationFire())
						break;
					
					//Check.
					if (equippedWeapon.HasAmmunition())
					{
						//Check.
						if (equippedWeapon.IsAutomatic())
							break;
							
						//Has fire rate passed.
						if (Time.time - lastShotTime > 60.0f / equippedWeapon.GetRateOfFire())
							Fire();
					}
					//Fire Empty.
					else
						FireEmpty();
					break;
				//Canceled.
				case {phase: InputActionPhase.Canceled}:
					//Stop Hold.
					holdingButtonFire = false;
					break;
			}
		}
		/// <summary>
		/// Reload.
		/// </summary>
		public void OnTryPlayReload(InputAction.CallbackContext context)
		{
			//Block while the cursor is unlocked.
			if (!cursorLocked)
				return;
			
			//Block.
			if (!CanPlayAnimationReload())
				return;

			
			//Switch.
			switch (context)
			{
				//Performed.
				case {phase: InputActionPhase.Performed}:
					//Play Animation.
					PlayReloadAnimation();
					break;
			}
		}

		IEnumerator Jump()
		{
			isGrounded = false;
			jumping = true;
            	// rigidbody.velocity = new Vector3(rigidbody.velocity.x, 0, rigidbody.velocity.z); // Reset vertical velocity before jump
            	// rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);			
			// jumpDuration = characterAnimator.GetCurrentAnimatorStateInfo(0).length * 10f;
			// Debug.Log("jump duration = " + jumpDuration);
			
			// rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Acceleration);
			// yield return new WaitForSeconds(jumpDuration);
			// jumping = false;
			yield break;
		}

		public void OnTryJump(InputAction.CallbackContext context)
		{
			if (!CanPlayJumping())
				return;

			//Switch.
			switch (context)
			{
				case {phase: InputActionPhase.Performed}:
				{
					// isJumping = true;
					// if (CanPlayJumping())
					// {
					Debug.Log("Jump input accepted");
					// jumpDuration = characterAnimator.GetCurrentAnimatorStateInfo(0).length;
					// Debug.Log("jump duration = " + jumpDuration);
					// jumping = true;

					// StartCoroutine(Jump());

					break;
					// }
				}

				// case {phase: InputActionPhase.Canceled}:
				// 	jumping = false;
				// 	Debug.Log("Input has now cancelled, jumping is false");
				// 	break;
					
				// 	Jump();
				// 	break;
									
			}
		}		

		public void OnTryPlayReloadForced(float beatMatchReloadMultiplier = 1f)
		{
			if (!cursorLocked || !CanPlayAnimationReload())
				return;
			
			PlayReloadAnimation(beatMatchReloadMultiplier);			
		}

		/// <summary>
		/// Inspect.
		/// </summary>
		public void OnTryInspect(InputAction.CallbackContext context)
		{
			//Block while the cursor is unlocked.
			if (!cursorLocked)
				return;
			
			//Block.
			if (!CanPlayAnimationInspect())
				return;
			
			//Switch.
			switch (context)
			{
				//Performed.
				case {phase: InputActionPhase.Performed}:
					//Play Animation.
					Inspect();
					break;
			}
		}
		/// <summary>
		/// Aiming.
		/// </summary>
		public void OnTryAiming(InputAction.CallbackContext context)
		{
			//Block while the cursor is unlocked.
			if (!cursorLocked)
				return;

			//Switch.
			switch (context.phase)
			{
				case InputActionPhase.Started:
					//Started.
					holdingButtonAim = true;
					break;
				case InputActionPhase.Canceled:
					//Canceled.
					holdingButtonAim = false;
					break;
			}
		}

		/// <summary>
		/// Holster.
		/// </summary>
		public void OnTryHolster(InputAction.CallbackContext context)
		{
			//Block while the cursor is unlocked.
			if (!cursorLocked)
				return;
			
			//Switch.
			switch (context.phase)
			{
				//Performed.
				case InputActionPhase.Performed:
					//Check.
					if (CanPlayAnimationHolster())
					{
						//Set.
						SetHolstered(!holstered);
						//Holstering.
						holstering = true;
					}
					break;
			}
		}
		/// <summary>
		/// Run. 
		/// </summary>
		public void OnTryRun(InputAction.CallbackContext context)
		{
			//Block while the cursor is unlocked.
			if (!cursorLocked)
				return;
			
			//Switch.
			switch (context.phase)
			{
				//Started.
				case InputActionPhase.Started:
					//Start.
					holdingButtonRun = true;
					break;
				//Canceled.
				case InputActionPhase.Canceled:
					//Stop.
					holdingButtonRun = false;
					break;
			}
		}
		/// <summary>
		/// Next Inventory Weapon.
		/// </summary>
		public void OnTryInventoryNext(InputAction.CallbackContext context)
		{
			//Block while the cursor is unlocked.
			if (!cursorLocked)
				return;
			
			//Null Check.
			if (inventory == null)
				return;
			
			//Switch.
			switch (context)
			{
				//Performed.
				case {phase: InputActionPhase.Performed}:
					//Get the index increment direction for our inventory using the scroll wheel direction. If we're not
					//actually using one, then just increment by one.
					float scrollValue = context.valueType.IsEquivalentTo(typeof(Vector2)) ? Mathf.Sign(context.ReadValue<Vector2>().y) : 1.0f;
					
					//Get the next index to switch to.
					int indexNext = scrollValue > 0 ? inventory.GetNextIndex() : inventory.GetLastIndex();
					//Get the current weapon's index.
					int indexCurrent = inventory.GetEquippedIndex();
					
					//Make sure we're allowed to change, and also that we're not using the same index, otherwise weird things happen!
					if (CanChangeWeapon() && (indexCurrent != indexNext))
						StartCoroutine(nameof(Equip), indexNext);
					break;
			}
		}
		
		public void OnLockCursor(InputAction.CallbackContext context)
		{
			//Switch.
			switch (context)
			{
				//Performed.
				case {phase: InputActionPhase.Performed}:
					//Toggle the cursor locked value.
					cursorLocked = !cursorLocked;
					//Update the cursor's state.
					UpdateCursorState();
					break;
			}
		}
		
		/// <summary>
		/// Movement.
		/// </summary>
		public void OnMove(InputAction.CallbackContext context)
		{
			//Read.
			axisMovement = cursorLocked ? context.ReadValue<Vector2>() : default;
		}
		/// <summary>
		/// Look.
		/// </summary>
		public void OnLook(InputAction.CallbackContext context)
		{
			//Read.
			axisLook = cursorLocked ? context.ReadValue<Vector2>() : default;
		}

		/// <summary>
		/// Called in order to update the tutorial text value.
		/// </summary>
		public void OnUpdateTutorial(InputAction.CallbackContext context)
		{
			//Switch.
			tutorialTextVisible = context switch
			{
				//Started. Show the tutorial.
				{phase: InputActionPhase.Started} => true,
				//Canceled. Hide the tutorial.
				{phase: InputActionPhase.Canceled} => false,
				//Default.
				_ => tutorialTextVisible
			};
		}

		#endregion

		#region ANIMATION EVENTS

		public override void EjectCasing()
		{
			//Notify the weapon.
			if(equippedWeapon != null)
				equippedWeapon.EjectCasing();
		}
		public override void FillAmmunition(int amount)
		{
			//Notify the weapon to fill the ammunition by the amount.
			if(equippedWeapon != null)
				equippedWeapon.FillAmmunition(amount);
		}
		
		public override void SetActiveMagazine(int active)
		{
			//Set magazine gameObject active.
			equippedWeaponMagazine.gameObject.SetActive(active != 0);
		}
		
		public override void AnimationEndedReload()
		{
			//Stop reloading!
			reloading = false;
		}

		public override void AnimationEndedInspect()
		{
			//Stop Inspecting.
			inspecting = false;
		}
		public override void AnimationEndedHolster()
		{
			//Stop Holstering.
			holstering = false;
		}

		#endregion

		#endregion
	}
}