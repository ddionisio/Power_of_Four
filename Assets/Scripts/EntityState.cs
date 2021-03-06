﻿public enum EntityState {
    Invalid = -1,
    Normal,
    Hurt,
    Dead,

    Spawn,
            
    //for enemies
    Immolate,
    Grabbed,
    Thrown,
    Knocked,

    // specific for player
    Lock,
    Victory,
    Charge,

    // special cases
    Final, //once the final boss is defeated.
    Exit //exit the level
}