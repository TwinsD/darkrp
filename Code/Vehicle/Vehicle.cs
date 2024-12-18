﻿using GameSystems.Jobs;

namespace Dxura.Darkrp;

public partial class Vehicle : Component, IRespawnable, IUse, IDescription, IDamageListener
{
	[Property] [Group( "Components" )] public Rigidbody Rigidbody { get; set; }
	[Property] [Group( "Components" )] public ModelRenderer Model { get; set; }

	/// <summary>
	/// An accessor for health component if we have one.
	/// </summary>
	[Property]
	public virtual HealthComponent HealthComponent { get; set; }

	/// <summary>
	/// What to spawn when we explode?
	/// </summary>
	[Property]
	[Group( "Effects" )]
	public GameObject Explosion { get; set; }

	[Property] [Group( "Effects" )] public float FireThreshold { get; set; } = 30f;
	[Property] [Group( "Effects" )] public GameObject Fire { get; set; }

	[Property] [Group( "Vehicle" )] public List<Wheel> Wheels { get; set; }
	[Property] [Group( "Vehicle" )] public List<PlayerSeat> Seats { get; set; }
	[Property] [Group( "Vehicle" )] public float Torque { get; set; } = 15000f;
	[Property] [Group( "Vehicle" )] public float BoostTorque { get; set; } = 20000f;
	[Property] [Group( "Vehicle" )] public float AccelerationRate { get; set; } = 1.0f;
	[Property] [Group( "Vehicle" )] public float DecelerationRate { get; set; } = 0.5f;
	[Property] [Group( "Vehicle" )] public float BrakingRate { get; set; } = 2.0f;
	[Property] [Group( "Description" )] public string DisplayName { get; set; }

	public VehicleInputState InputState { get; } = new();

	private float _currentTorque;
	private GameObject _fire;

	protected override void OnFixedUpdate()
	{
		// Evaluate all input-driving seats, and if all of them have nobody in it, reset the input
		// Otherwise the vehicle will just go forever
		if ( Seats.Where( x => x.HasInput ).All( x => !x.Player.IsValid() ) )
		{
			InputState.Reset();
		}

		if ( IsProxy )
		{
			return;
		}

		var torque = InputState.isBoosting ? BoostTorque : Torque;
		var verticalInput = InputState.direction.x;
		var targetTorque = verticalInput * torque;

		var isBraking = Math.Sign( verticalInput * _currentTorque ) == -1;
		var isDecelerating = verticalInput == 0;

		var lerpRate = AccelerationRate;
		if ( isBraking )
		{
			lerpRate = BrakingRate;
		}
		else if ( isDecelerating )
		{
			lerpRate = DecelerationRate;
		}

		_currentTorque = _currentTorque.LerpTo( targetTorque, lerpRate * Time.Delta );

		foreach ( var wheel in Wheels )
		{
			wheel.ApplyMotorTorque( _currentTorque );
		}

		var groundVel = Rigidbody.Velocity.WithZ( 0f );
		if ( verticalInput == 0f && groundVel.Length < 32f )
		{
			var z = Rigidbody.Velocity.z;
			Rigidbody.Velocity = Vector3.Zero.WithZ( z );
		}

		var currentUp = Transform.World.NormalToWorld( Vector3.Up );
		var alignment = Vector3.Dot( Vector3.Up, currentUp );

		if ( alignment < 0.6f )
		{
			var desiredRotation = Rotation.From( 0, Transform.Rotation.Angles().yaw, 0 );
			var rotationDifference = desiredRotation * Transform.Rotation.Inverse;

			ToAngleAxis( rotationDifference, out var angle, out var axis );

			angle = MathF.Min( angle, 180f );

			var alignSpeed = 1f;
			var force = 3f;

			Rigidbody.AngularVelocity = axis * angle * force * alignSpeed * Time.Delta;
		}
	}

	/// <summary>
	/// Converts a quaternion to an angle-axis representation.
	/// I did not create this method.
	/// </summary>
	/// <param name="rotation">The quaternion to convert.</param>
	/// <param name="angle">The output angle in degrees.</param>
	/// <param name="axis">The output axis of rotation.</param>
	private void ToAngleAxis( Rotation rotation, out float angle, out Vector3 axis )
	{
		// Normalize the quaternion to ensure it represents a valid rotation
		var normalized = rotation.Normal;

		// Calculate the angle
		angle = 2.0f * (float)Math.Acos( normalized.w );

		// Calculate the axis
		var sinThetaOver2 = (float)Math.Sqrt( 1.0f - normalized.w * normalized.w );

		if ( sinThetaOver2 > 0.0001f )
		{
			axis = new Vector3( normalized.x, normalized.y, normalized.z ) / sinThetaOver2;
		}
		else
		{
			axis = new Vector3( 1, 0, 0 );
		}

		axis = axis.Normal;
		angle = angle * (180.0f / (float)Math.PI);
	}

	public void OnKill( DamageInfo damageInfo )
	{
		foreach ( var seat in Seats )
		{
			seat.Eject();
		}

		Explosion?.Clone( WorldPosition );
		GameObject.Destroy();
	}

	public UseResult CanUse( Player player )
	{
		// You're already in a vehicle somehow
		if ( player.CurrentSeat.IsValid() && player.CurrentSeat.Vehicle == this )
		{
			return false;
		}

		// Seats are all filled
		if ( Seats.All( x => !x.CanEnter( player ) ) )
		{
			return $"{DisplayName} is full";
		}

		return true;
	}

	public void OnUse( Player player )
	{
		// Already in the vehicle, fuck off
		if ( Seats.FirstOrDefault( x => x.CanEnter( player ) ) is { } availableSeat )
		{
			availableSeat.Enter( player );
		}
	}

	public void OnDamaged( DamageInfo damageInfo )
	{
		Log.Info( HealthComponent.Health );
		if ( HealthComponent.Health < FireThreshold && !_fire.IsValid() )
		{
			_fire = Fire.Clone( GameObject, Vector3.Zero, Rotation.Identity, Vector3.One );
		}
	}

	protected override void OnDestroy()
	{
		if ( _fire.IsValid() )
		{
			_fire.Destroy();
		}
	}
}
