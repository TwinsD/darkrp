using Dxura.Darkrp;
using Dxura.Darkrp;
using Dxura.Darkrp;
using Sandbox.Events;

namespace Dxura.Darkrp;

/// <summary>
/// Controls what equipment can be dropped by players, either when killed or with a key bind.
/// </summary>
public partial class EquipmentDropper : Component,
	IGameEventHandler<KillEvent>
{
	/// <summary>
	/// Which categories can we drop?
	/// </summary>
	[Property]
	public List<EquipmentSlot> Categories { get; set; } = new();

	/// <summary>
	/// Can we drop this weapon?
	/// </summary>
	/// <param name="player"></param>
	/// <param name="weapon"></param>
	/// <returns></returns>
	public bool CanDrop( Player player, Equipment weapon )
	{
		if (!weapon.Resource.CanDrop || weapon.Resource.Slot == EquipmentSlot.Melee )
		{
			return false;
		}

		if ( Categories.Count == 0 )
		{
			return true;
		}

		return Categories.Contains( weapon.Resource.Slot );
	}

	void IGameEventHandler<KillEvent>.OnGameEvent( KillEvent eventArgs )
	{
		return;
		
		// We don't want to drop anything on death I assume. TODO: Drop stashed entities 
		
		// if ( !Networking.IsHost )
		// {
		// 	return;
		// }
		//
		// var player = GameUtils.GetPlayerFromComponent( eventArgs.DamageInfo.Victim );
		// if ( !player.IsValid() )
		// {
		// 	return;
		// }
		//
		// var droppable = player.Inventory.Equipment
		// 	.Where( x => CanDrop( player, x ) )
		// 	.OrderByDescending( x => x.Resource.Price )
		// 	.ToArray();
		//
		// if ( LimitedDropOnDeath )
		// {
		// 	// One weapon, one util, all special
		//
		// 	var weapon = droppable
		// 		.FirstOrDefault( x =>
		// 			x.Resource.Slot is EquipmentSlot.Primary or EquipmentSlot.Secondary or EquipmentSlot.Melee );
		//
		// 	var util = droppable
		// 		.FirstOrDefault( x => x.Resource.Slot is EquipmentSlot.Utility );
		//
		// 	var special = droppable
		// 		.Where( x => x.Resource.Slot is EquipmentSlot.Special );
		//
		// 	if ( weapon is not null )
		// 	{
		// 		player.Inventory.Drop( weapon );
		// 	}
		//
		// 	if ( util is not null )
		// 	{
		// 		player.Inventory.Drop( util );
		// 	}
		//
		// 	foreach ( var equipment in special )
		// 	{
		// 		player.Inventory.Drop( equipment );
		// 	}
		// }
		// else
		// {
		// 	foreach ( var equipment in droppable )
		// 	{
		// 		player.Inventory.Drop( equipment );
		// 	}
		// }
		//
		// player.Inventory.Clear();
	}
}
