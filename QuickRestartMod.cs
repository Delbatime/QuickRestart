using UnityEngine;
using BepInEx;

namespace DeltaTime {
    [BepInPlugin("Deltatime_QuickRestart", "QuickRestart", versionNumber)]
    public class QuickRestartMod : BaseUnityPlugin {

        //VersionNumber is Major.Minor.Patch
        //BuildNumber is only added to the file version in windows
        public const string versionNumber = "0.0.1";
        public const string buildNumber = "123";

        //Apply hooks
        QuickRestartMod() {
            On.RainWorldGame.GoToDeathScreen += RainWorldGame_GoToDeathScreen;
            On.HUD.KarmaMeter.UpdateGraphic += KarmaMeter_UpdateGraphic;
            On.Player.Update += Temp;
        }

        void Temp(On.Player.orig_Update orig, Player self, bool eu) {
            orig(self, eu);
            Debug.Log(self.abstractCreature.pos);
        }

        void KarmaMeter_UpdateGraphic(On.HUD.KarmaMeter.orig_UpdateGraphic orig, HUD.KarmaMeter self) {
            orig(self);
            if (!(self?.hud?.owner is Player)) {
                Debug.Log("[QUICKRESTART - ERROR] Could not get player from Karma HUD graphic");
                return;
            }
            Player p = self.hud.owner as Player;
            if (p?.abstractCreature?.world?.game == null && p.abstractCreature.world.game.IsStorySession) {
                Debug.Log("[QUICKRESTART - ERROR] Could not get storygamesession.");
                return;
            }   
            self.showAsReinforced = (p.abstractCreature.world.game.session as StoryGameSession).saveState.deathPersistentSaveData.reinforcedKarma;
        }

        void RainWorldGame_GoToDeathScreen(On.RainWorldGame.orig_GoToDeathScreen orig, RainWorldGame self) {
            if (self?.world?.rainCycle.RainGameOver == true) {
                orig(self);
                return;
            } else {
                if (self.world == null) {
                    Debug.Log("[QUICKRESTART - ERROR] Could not find world");
                    orig(self);
                    return;
                } else if (self.world.rainCycle == null) {
                    Debug.Log("[QUICKRESTART - ERROR] Could not find raincycle.");
                    orig(self);
                    return;
                } else if (self.world.game == null) {
                    Debug.Log("[QUICKRESTART - ERROR] Could not find world.game.");
                }

                Revive(self.Players[0]);

                self.world.game.cameras[0].hud.textPrompt.gameOverMode = false;
                self.world.game.cameras[0].hud.textPrompt.playGameOverSound = false;

                if (self.world.game.IsStorySession) {
                    DeathPersistentSaveData saveData = self.world.game.GetStorySession.saveState.deathPersistentSaveData;
                    if (!saveData.reinforcedKarma) {
                        if (saveData.karma != 0) {
                            --saveData.karma;
                            Debug.Log("[QUICKRESTART - DEBUG] Decreased Karma");
                        } else {
                            Debug.Log("[QUICKRESTART - DEBUG] Karma is already at lowest!");
                        }
                        
                    } else {
                        saveData.reinforcedKarma = !saveData.reinforcedKarma;
                        Debug.Log("[QUICKRESTART - DEBUG] Removed kamra reinforcement.");
                    }
                    //Update the karma meter graphic
                    self.world.game.cameras[0].hud.karmaMeter.UpdateGraphic();
                } else {
                    Debug.Log("[QUICKRESTART - WARNING] Session is not a story game session, cannot decrease Karma.");
                }

                //Remove the pause menu.
                //self.world.game.pauseMenu = null;

                //Teleport the player over to the new room?
                //The if statement can be removed after debugging.
                /*
                if ((self.Players[0].realizedCreature as Player).karmaFlowerGrowPos != null) {
                    Debug.Log($"[QUICKRESTART - DEBUG] Current player pos : {self.Players[0].pos}");
                    WorldCoordinate newPos = (self.Players[0].realizedCreature as Player).karmaFlowerGrowPos ?? self.Players[0].pos;
                    self.Players[0].pos = newPos;
                    Debug.Log($"[QUICKRESTART - DEBUG] Teleport to karma flower pos at {(self.Players[0].realizedCreature as Player).karmaFlowerGrowPos}");
                    /*
                    if (self.Players[0].pos.room != ((self.Players[0].realizedCreature as Player).karmaFlowerGrowPos ?? self.Players[0].pos).room) {
                        Debug.Log("[QUICKRESTART - DEBUG] Moving player to new room");
                        self.Players[0].realizedCreature.LoseAllGrasps();
                        self.Players[0].realizedCreature.Destroy();
                        self.Players[0].Move((self.Players[0].realizedCreature as Player).karmaFlowerGrowPos ?? self.Players[0].pos);
                        self.Players[0].world.GetAbstractRoom(self.Players[0].pos.room).RealizeRoom(self.world, self.world.game);
                        self.world.game.cameras[0].ChangeRoom(self.Players[0].world.GetAbstractRoom(self.Players[0].pos.room).realizedRoom, 0);
                    }
                    if (self.Players[0].world.game.cameras[0].ViewedByCameraPosition(self.Players[0].pos.Tile.ToVector2()) != -1 && self.Players[0].world.game.cameras[0].ViewedByCameraPosition(self.Players[0].pos.Tile.ToVector2()) != self.Players[0].world.game.cameras[0].currentCameraPosition) {
                        self.Players[0].world.game.cameras[0].MoveCamera(self.Players[0].world.game.cameras[0].ViewedByCameraPosition(self.Players[0].pos.Tile.ToVector2()));
                        Debug.Log("[QUICKRESTART - DEBUG] Moving camera to player's position");
                    }

                } else {
                    Debug.Log("[QUICKRESTART - DEBUG] Karma flower grow pos is null");
                }
                Debug.Log("[QUICKRESTART - TEMP] New player's position " + self.Players[0].pos.Tile + " | " + self.Players[0].realizedCreature.coord.Tile);
                */
            }
            return;
        }

        void Revive(AbstractCreature creature) {
            //Null checks
            if (creature == null) {
                Debug.Log("[QUICKRESTART - ERROR] Creature is null");
                return;
            }
            Creature realizedCreature = creature.realizedCreature;
            if (realizedCreature == null) {
                Debug.Log("[QUICKRESTART - ERROR] Failed to get realized creature.");
                return;
            }

            if (creature.slatedForDeletion == true) {
                Debug.Log("[INFO] - Slated for deletion");
            }

            //Initial revive
            creature.state.alive = true;
            realizedCreature.dead = false;
            realizedCreature.AllGraspsLetGoOfThisObject(true);
            
            //Remove creature from den if in one.
            bool revivedFromDen = false; //Whether or not the creature has been revived from a den (if true then this creature should not be added to the room again since that will be done by the game automatically).
            if (creature.Room.entitiesInDens.Contains(creature)) {
                creature.Room.MoveEntityOutOfDen(creature);
                Debug.Log($"[QUICKRESTART - DEBUG] Removed {creature} from den.");
                revivedFromDen = true;
            }

            //Give the realized creature the room it is currently in.
            if (realizedCreature.room == null) {
                realizedCreature.room = creature.Room.realizedRoom;
                Debug.Log($"[QUICKRESTART - DEBUG] Gave realized {creature} a room.");
            }
            
            //Add abstract creature back to the room if it was removed
            if (!creature.Room.creatures.Contains(creature)) {
                creature.Room.AddEntity(creature);
                Debug.Log($"[QUICKRESTART - DEBUG] Added Abstract {creature} to room creature list.");
            }

            //Add realized creature back to the room if it was deleted
            if (!realizedCreature.room.physicalObjects[realizedCreature.collisionLayer].Contains(realizedCreature) && !revivedFromDen && realizedCreature.slatedForDeletetion) {
                realizedCreature.slatedForDeletetion = false;
                creature.Room.realizedRoom.AddObject(realizedCreature);
                Debug.Log($"[QUICKRESTART - DEBUG] Re-added deleted {creature} to room.");
                //Reset graphics module
                realizedCreature.graphicsModule.Reset();
                Debug.Log($"[QUICKRESTART - DEBUG] Reset graphics module for realized {creature}.");

            }

            //In the case of wormgrass, prevent creature from falling through ground
            if (!realizedCreature.CollideWithTerrain) {
                realizedCreature.CollideWithTerrain = true;
                Debug.Log($"[QUICKRESTART - DEBUG] Enabled {creature}'s collision with terrain.");
            }

            //Remove stun (for vultures)
            if (realizedCreature.stun != 0) {
                Debug.Log($"[QUICKRESTART - DEBUG] Removed stun from {creature}.");
                realizedCreature.stun = 0;
            }

            //Ungrasp wormgrass from creature.
            foreach (IAccessibilityModifier o in realizedCreature.room.accessModifiers) {
                if (o != null && o is WormGrass) {
                    foreach (WormGrass.WormGrassPatch patch in (o as WormGrass).patches) {
                        foreach (WormGrass.WormGrassPatch.CreatureAndPull creatureAndPull in patch.trackedCreatures) {
                            if (creatureAndPull.creature == creature.realizedCreature) {
                                Debug.Log($"[QUICKSTART - DEBUG] {creature} is grabbed by wormgrass, releasing.");
                                creatureAndPull.bury = 0f;
                                creatureAndPull.pull = 0f;
                            }
                        }
                    }
                }
            }

            //Release creature from leviathans and DaddyLongLegs/BrotherLongLegs
            //For beastmaster compatability has to use the physical objects instead of AbstractRoom.creatures...
            foreach (System.Collections.Generic.List<PhysicalObject> layer in realizedCreature.room.physicalObjects) {
                foreach (PhysicalObject c in layer) {
                    if (c.abstractPhysicalObject.type == AbstractPhysicalObject.AbstractObjectType.Creature) {
                        if (c is Creature) {
                            Creature realizedC = c as Creature;
                            if (realizedC.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.BigEel) {
                                Debug.Log("[QUICKSTART - DEBUG] BigEel found in room.");
                                BigEel eel = (realizedC as BigEel);
                                if (eel != null) {
                                    System.Collections.Generic.List<BigEel.ClampedObject> clampsToRemove = new System.Collections.Generic.List<BigEel.ClampedObject>();
                                    foreach (BigEel.ClampedObject clamp in eel.clampedObjects) {
                                        if (clamp.chunk.owner == realizedCreature) {
                                            Debug.Log($"[QUICKSTART - DEBUG] {creature} is clamped by BigEel, releasing.");
                                            clampsToRemove.Add(clamp);
                                        }
                                    }
                                    foreach (BigEel.ClampedObject clamp in clampsToRemove) {
                                        eel.clampedObjects.Remove(clamp);
                                    }
                                    clampsToRemove.Clear();
                                }
                            } else if (realizedC.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.DaddyLongLegs || realizedC.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.BrotherLongLegs) {
                                Debug.Log("[QUICKSTART - DEBUG] Found DaddyLongLegs/BrotherLongLegs in room.");
                                DaddyLongLegs longLeg = realizedC as DaddyLongLegs;
                                System.Collections.Generic.List<DaddyLongLegs.EatObject> objectsToRemove = new System.Collections.Generic.List<DaddyLongLegs.EatObject>();
                                Debug.Log("[TEMP]: " + longLeg.eatObjects.Count);
                                foreach(DaddyLongLegs.EatObject eatObj in longLeg.eatObjects) {
                                    if (eatObj.chunk.owner == realizedCreature)  {
                                        Debug.Log($"[QUICKSTART - DEBUG] {creature} is an eatObject of a DaddyLongLegs/BrotherLongLegs, releasing.");
                                        objectsToRemove.Add(eatObj);
                                    }
                                }
                                foreach(DaddyLongLegs.EatObject obj in objectsToRemove) {
                                    longLeg.eatObjects.Remove(obj);
                                }
                            }
                        }
                    }
                }
            }

            //Slugcat only!
            if (creature.realizedCreature is Player) {
                //Resets slugcat's animation so they can stand up properly
                Debug.Log("[QUICKRESTART - DEBUG] Set animation to none for PLAYER.");
                (creature.realizedCreature as Player).bodyMode = Player.BodyModeIndex.Crawl;
                (creature.realizedCreature as Player).animation = Player.AnimationIndex.None;

                //Resets slugcat's collision layer.
                if (realizedCreature.collisionLayer != 1) {
                    Debug.Log("[QUICKRESTART - DEBUG] Reset collision layer for PLAYER");
                    realizedCreature.room.ChangeCollisionLayerForObject(realizedCreature, 1);
                }

            }

            Debug.Log($"[QUCIKRESTART - DEBUG] Revived {creature}");
        }
    }
}