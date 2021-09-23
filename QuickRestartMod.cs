using UnityEngine;
using BepInEx;

namespace DeltaTime {
    [BepInPlugin("Deltatime_QuickRestart", "QuickRestart", versionNumber)]
    public class QuickRestartMod : BaseUnityPlugin {

        public const string versionNumber = "0.0.1";
        public const string buildNumber = "26";

        //Apply hooks
        QuickRestartMod() {
            On.RainWorldGame.GoToDeathScreen += RainWorldGame_GoToDeathScreen;
            //On.Player.Update += temp;
        }
    
        void temp (On.Player.orig_Update orig, Player self, bool eu) {
            orig(self, eu);
            Debug.Log(self.standing);
        }

        void RainWorldGame_GoToDeathScreen(On.RainWorldGame.orig_GoToDeathScreen orig, RainWorldGame self) {
            if (self?.world?.rainCycle.RainGameOver == true) {
                orig(self);
                return;
            } else {
                if (self.world == null) {
                    Debug.Log("Could not find world");
                    orig(self);
                    return;
                } else if (self.world.rainCycle == null) {
                    Debug.Log("Could not find raincycle.");
                    orig(self);
                    return;
                }
                Debug.Log("Player has attempted restart.");
                //Revive player somehow?
                PlayerUnDie(self.Players[0]);

                self.world.game.cameras[0].hud.textPrompt.gameOverMode = false;
                self.world.game.cameras[0].hud.textPrompt.playGameOverSound = false;
                //Debug.Log("Attempting to travel to new room");
                //WarpModMenu.newRoom = self.world.game.startingRoom;
            }
            return;
        }

        void PlayerUnDie(AbstractCreature creature) {
            if (creature == null) {
                Debug.Log("Undie! Failed");
                return;
            }
            Creature realizedCreature = creature.realizedCreature;
            if (realizedCreature == null) {
                Debug.Log("Undie! Failed to get realized creature.");
                return;
            }
            //Remove creature from Dens

            //Initial revive
            creature.state.alive = true;
            realizedCreature.dead = false;
            //realizedCreature.AllGraspsLetGoOfThisObject(true);
            //realizedCreature.stun = 0;
            if (realizedCreature.inShortcut) {
                //creature.realizedCreature.SpitOutOfShortCut();
                creature.IsExitingDen();
                return;
            }
            //In case the creature is deleted/removed from the room
            if (realizedCreature.room == null) {
                Debug.Log("Undie! Failed to get room creature is in. Adding creature back to room");
                realizedCreature.slatedForDeletetion = false;
                creature.Room.realizedRoom.AddObject(realizedCreature); //Maybe this will work?
            }
            //In the case of wormgrass, prevent creature from falling through ground
            if (!realizedCreature.CollideWithTerrain) {
                realizedCreature.CollideWithTerrain = true;
            }
            //Slugcat only (for spears hitting)
            if (creature.realizedCreature is Player) {
                Debug.Log("Undie! Creature is slugcat");
                (creature.realizedCreature as Player).bodyMode = Player.BodyModeIndex.Crawl;
                (creature.realizedCreature as Player).animation = Player.AnimationIndex.None;
            }
            //Ungrasp wormgrass from creature.
            /*
            foreach(IAccessibilityModifier o in creatureRoom.accessModifiers) {
                if (o != null && o is WormGrass) {
                    foreach (WormGrass.WormGrassPatch patch in (o as WormGrass).patches) {
                        foreach (WormGrass.WormGrassPatch.CreatureAndPull creatureAndPull in patch.trackedCreatures) {
                            if (creatureAndPull.creature == creature.realizedCreature) {
                                Debug.Log("FOUND GRABBED CREATURE");
                                //Copied over and modified from  private void WormGrass.WormGrassPatch.LoseGrip(WormGrass.WormGrassPatch.CreatureAndPull)
                                if (creatureAndPull.bury <= 0f) {
                                    creatureAndPull.bury = Mathf.Max(0f, creatureAndPull.bury - 0.1f);
                                    creatureAndPull.pull = Mathf.Max(0f, creatureAndPull.pull - 0.1f);
                                }
                                //End of copy.
                            } else if (creatureAndPull.creature is Player) {
                                Debug.Log("FOUND PLAYER, BUT IS NOT SAME AS GRABBED CREATURE.");
                            }
                        }
                    }
                }
            }*/
            Debug.Log("Undie!" + creature.type.ToString());
        }

        [System.Obsolete("Depricated: Unused")]
        void Unstick(AbstractPhysicalObject c) {
            if (c.Room == null) {
                return;
            }
            foreach (AbstractCreature other in c.Room.creatures) {
                System.Collections.Generic.List<AbstractPhysicalObject.AbstractObjectStick> temp = new System.Collections.Generic.List<AbstractPhysicalObject.AbstractObjectStick>();
                foreach(AbstractPhysicalObject.AbstractObjectStick stick in other.stuckObjects) {
                    if (stick.B == c) {
                        temp.Add(stick);
                    }
                }
                foreach(AbstractPhysicalObject.AbstractObjectStick matchingStick in temp) {
                    other.stuckObjects.Remove(matchingStick);
                    Debug.Log("Unsticking creature!");
                }
            }
        }

    }
}
