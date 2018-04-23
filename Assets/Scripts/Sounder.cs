using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class Sounder : Manager<Sounder> {

    public AudioClip WowSound;
    public AudioClip GloriousSound;
    public AudioClip LudicrousSound;
    public AudioClip QuickSound;
    public AudioClip LongSound;
    public AudioClip SharpshooterSound;
    public AudioClip RiskySound;
    public AudioClip MoveSound;

}