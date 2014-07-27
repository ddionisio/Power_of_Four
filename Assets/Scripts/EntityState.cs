public enum EntityState {
    Invalid = -1,
    Normal,
    Hurt,
    Dead,
            
    //for enemies
    Immolate,
    Grabbed,
    Thrown,
    Knocked,

    BossEntry, //boss enters
    RespawnWait,

    // specific for player
    Lock,
    Victory,

    // special cases
    Final, //once the final boss is defeated.
    Exit //exit the level
}