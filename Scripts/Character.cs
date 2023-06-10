using System;
using Godot;
using Godot.Collections;
using HorrorGame.Scripts.Enums;

namespace HorrorGame.Scripts;

public partial class Character : CharacterBody3D
{
	[Export] public int Sensitivivity { get; set; } = 3;
	public const float Speed = 5.0f;
	public const float CrouchSpeed = 2.0f;
	public const float JumpVelocity = 4.5f;
	public float CurrentFeetSpeed { get; set; } = Speed;
	public bool IsCrouched { get; private set; }
	public Camera3D Camera3d { get; private set; }
	public AnimationPlayer AnimationPlayer { get; private set; }
	public bool IsFlashlightShown { get; set; }

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	public override void _Ready()
	{
		Camera3d = GetNode<Camera3D>("Camera3D");
		AnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _PhysicsProcess(double delta)
	{
		var velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
			velocity.Y -= _gravity * (float)delta;

		// Handle Jump.
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
			velocity.Y = JumpVelocity;

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		var inputDir = Input.GetVector(EInputKeyboard.MoveLeft.ToString(), EInputKeyboard.MoveRight.ToString(), EInputKeyboard.MoveForward.ToString(), EInputKeyboard.MoveBackwards.ToString());
		var direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * CurrentFeetSpeed;
			velocity.Z = direction.Z * CurrentFeetSpeed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, CurrentFeetSpeed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, CurrentFeetSpeed);
		}

		Velocity = velocity;
		MoveAndSlide();
		HandleCrouch();
		HandleFlashlight();
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventMouseMotion eMouseMotion) return;

		// 2D relative motion is 1000 units 3D motion
		// Player Rotation
		var rotY = Rotation.Y - (eMouseMotion.Relative.X / 1000 * Sensitivivity);
		Rotation = new Vector3(Rotation.X, rotY, Rotation.Z);
		
		// Camera Rotation
		var rotX = Camera3d.Rotation.X - (eMouseMotion.Relative.Y / 1000 * Sensitivivity);
		var clampRotationX = Math.Clamp(rotX, Mathf.Pi / -2f, Mathf.Pi / 2f);
		Camera3d.Rotation = new Vector3(clampRotationX, Camera3d.Rotation.Y, Camera3d.Rotation.Z);
	}

	private void HandleCrouch()
	{
		if (Input.IsActionPressed(EInputKeyboard.Crouch.ToString()))
		{
			if (IsCrouched) return;
			AnimationPlayer.Play("Crouch");
			IsCrouched = true;
			CurrentFeetSpeed = CrouchSpeed;
		}
		else
		{
			if (!IsCrouched) return;
			
			var spaceState = GetWorld3D().DirectSpaceState;
			var result = spaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(Position, Position + Vector3.Up * 2f, 1, new Array<Rid>(new []{GetRid()})));
			if (result.Count == 0)
			{
				AnimationPlayer.Play("UnCrouch");
				IsCrouched = false;
				CurrentFeetSpeed = Speed;
			}
		}
	}

	private void HandleFlashlight()
	{
		if (Input.IsActionJustPressed(EInputKeyboard.Flashlight.ToString()))
		{
			AnimationPlayer.Play(IsFlashlightShown ? "HideFlashlight" : "ShowFlashlight");
			IsFlashlightShown = !IsFlashlightShown;
		}
	}
}