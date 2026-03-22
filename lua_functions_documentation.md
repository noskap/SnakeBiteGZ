# Lua Functions Documentation

## GZCommon (class)
- `GZCommon.CallAlertSiren()`
- `GZCommon.CallAlertSirenCheck()`
- `GZCommon.CallCautionSiren()`
- `GZCommon.CallHeliEscapeBGM()`
- `GZCommon.CallMonologueHostage(CharacterID, HostageVoiceType, DataIdentifierName, OffTrapName)`
- `GZCommon.CallSearchTarget()`
- `GZCommon.ChangeAntiAir()`
- `GZCommon.CheckClearRankReward(missionId, ClearRankRewardList)`
- `GZCommon.CheckContinueHostageRegister(ContinueHostageRegisterList)`
- `GZCommon.CheckEnemyLaidonHeliNoAnnounce(CharacterID)`
- `GZCommon.CheckReward_AllChicoTape()`
- `GZCommon.Common_CenterBigGateVehicle()`
- `GZCommon.Common_CenterBigGateVehicleEndCPRadio()`
- `GZCommon.Common_CenterBigGateVehicleInit()`
- `GZCommon.Common_CenterBigGateVehicleSetup(CpId, VehicleRouteInfoName, VehicleRouteName01, VehicleRouteName02, VehicleRouteNodeIndex01, VehicleRouteNodeIndex02)`
- `GZCommon.Common_CenterBigGate_Close()`
- `GZCommon.Common_CenterBigGate_Open()`
- `GZCommon.Common_CenterBigGate_OpenTime()`
- `GZCommon.Common_ChicoPazDead()`
- `GZCommon.CpRadioFadeOut(CharacterID)`
- `GZCommon.DromItemFromPlayer(itemId, index)`
- `GZCommon.EnemyInterrogation()`
- `GZCommon.EnemyLaidonHeliNoAnnounceSet(CharacterID)`
- `GZCommon.EnemyLaidonVehicle()`
- `GZCommon.Fence_SE()`
- `GZCommon.ForceMove_Vehicle(VehicleId, Pos, Angle)`
- `GZCommon.MissionCleanup()`
- `GZCommon.MissionPrepare()`
- `GZCommon.MissionSetup()`
- `GZCommon.NormalHostageRecovery(HostageCharacterID)`
- `GZCommon.OnStartMotherBaseDevise()`
- `GZCommon.OnStartWarningFlare()`
- `GZCommon.OutsideAreaCamera()`
- `GZCommon.OutsideAreaCamera_Human(CharacterId, Options)`
- `GZCommon.OutsideAreaCamera_Vehicle(VehicleId, CharaId, options)`
- `GZCommon.OutsideAreaEffectDisable()`
- `GZCommon.OutsideAreaEffectEnable()`
- `GZCommon.OutsideAreaGameRealizeCharacter(VehicleID, CharaId, Options, EnemyDisable)`
- `GZCommon.OutsideAreaVoiceEnd()`
- `GZCommon.OutsideAreaVoiceStart()`
- `GZCommon.PlayCameraAnimationOnChicoPazDead(characterId)`
- `GZCommon.PlayCameraOnCommonCharacterGameOver(characterId, OffSetPos)`
- `GZCommon.PlayCameraOnCommonCharacterOutsideArea(characterId, OffSetPos)`
- `GZCommon.PlayCameraOnCommonHelicopterGameOver()`
- `GZCommon.PlayerGetOffCargo()`
- `GZCommon.PlayerGetOffVehicle()`
- `GZCommon.PlayerRideOnCargo()`
- `GZCommon.PlayerRideOnVehicle()`
- `GZCommon.Radio_pleaseLeaveHeli()`
- `GZCommon.Register(script, manager, type)`
- `GZCommon.ScoreRankTableSetup(missionId)`
- `GZCommon.SearchTargetCharacterSetup(manager, CharacterID)`
- `GZCommon.SetGameStatusForDemoTransition()`
- `GZCommon.ShowCommonAnnounceLog(LangId)`
- `GZCommon.StopAlertSirenCheck()`
- `GZCommon.StopHeliEscapeBGM()`
- `GZCommon.StopSirenForcibly()`
- `GZCommon.StopSirenNormal()`
- `GZCommon.SupporHeliCloseDoor()`
- `GZCommon.SupporHeliDeparture()`
- `GZCommon.SupporHeliLandingZone()`
- `GZCommon.commonEsrConClean()`
- `GZCommon.commonFunc()`
- `GZCommon.onEnterPlayerAreaTrap()`

## TppClock (class)
- `TppClock.GetTime(style)`
- `TppClock.GetTimeOfDay()`
- `TppClock.RegisterClockMessage(time, message, esmType)`
- `TppClock.SetTime(time)`
- `TppClock.Start()`
- `TppClock.Stop()`
- `TppClock._ParseTimeString(time, style)`

## TppCommon (class)
- `TppCommon.DeprecatedFunction(useFunc)`
- `TppCommon.Register(script, manager, type)`

## TppData (class)
- `TppData.Disable(characterID)`
- `TppData.Enable(characterID)`
- `TppData.GetArgument(argNum)`
- `TppData.GetData(dataName, type)`
- `TppData.GetPosition(dataName)`
- `TppData.GetRotation(dataName)`
- `TppData.HideModel(dataName)`
- `TppData.ShowModel(dataName)`
- `TppData._DoModel(dataName, isDo)`

## TppDemo (class)
- `TppDemo.OnDemoDisable(demoName)`
- `TppDemo.OnDemoEnd(demoName)`
- `TppDemo.OnDemoInterrupt(demoName)`
- `TppDemo.OnDemoPlay(demoName)`
- `TppDemo.OnDemoSkip(demoName)`
- `TppDemo.Play(demoID, funcs, flags)`
- `TppDemo.PlayOpening(funcs)`
- `TppDemo.Register(demoList)`
- `TppDemo.Start()`
- `TppDemo._DoMessage(demoName, funcName)`
- `TppDemo._OnDemoDisable(manager, message, demoName)`
- `TppDemo._OnDemoEnd(manager, message, demoName)`
- `TppDemo._OnDemoInterrupt(manager, message, demoName)`
- `TppDemo._OnDemoPlay(manager, message, demoName)`
- `TppDemo._OnDemoSkip(manager, message, demoName)`
- `TppDemo.onOpeningDemoFadeIn()`
- `TppDemo.onOpeningDemoStart()`
- `TppDemo.onOpeningDemoStartTransition()`

## TppEffect (class)
- `TppEffect.CreateEffect(effectInstanceName, options)`
- `TppEffect.DisableAreaFog(dataName)`
- `TppEffect.DisableDamageFilter()`
- `TppEffect.EnableAreaFog(dataName)`
- `TppEffect.EnableDamageFilter()`
- `TppEffect.HideEffect(effectInstanceName, options)`
- `TppEffect.ShowEffect(effectInstanceName, options)`
- `TppEffect._SetEnableAreaFog(dataName, enable)`
- `TppEffect._SetVisible(effectInstanceName, useGroup, visible)`

## TppEnemy (class)
- `TppEnemy.ChangeRoute(cpID, enemyID, routeSetName, routeName, nodeNum, options)`
- `TppEnemy.ChangeRouteSet(cpID, routeSetName, options)`
- `TppEnemy.DisableCombatLocator(cpID, combatLocator)`
- `TppEnemy.DisableDemoEnemies(demoName)`
- `TppEnemy.DisableEnemyReaction()`
- `TppEnemy.DisableGuardTarget(cpID, guardTarget)`
- `TppEnemy.DisableRoute(cpID, routeName, options)`
- `TppEnemy.DisableSecurityCamera()`
- `TppEnemy.EnableCombatLocator(cpID, combatLocator)`
- `TppEnemy.EnableDemoEnemies(demoName)`
- `TppEnemy.EnableEnemyReaction()`
- `TppEnemy.EnableGuardTarget(cpID, guardTarget)`
- `TppEnemy.EnableRoute(cpID, routeName, options)`
- `TppEnemy.EnableSecurityCamera()`
- `TppEnemy.GetCharacter(characterID)`
- `TppEnemy.GetCharacterOnRoute(cpID, routeID)`
- `TppEnemy.GetEnemyCountNearEnemy(characterID, radius)`
- `TppEnemy.GetEnemyStatus(characterID)`
- `TppEnemy.GetEnemyType(characterID)`
- `TppEnemy.GetHostageStatus(characterID)`
- `TppEnemy.GetPhase(cpID)`
- `TppEnemy.GetPosition(characterID)`
- `TppEnemy.GetRotation(characterID)`
- `TppEnemy.IsWithinDistance(chara01, chara02, distance)`
- `TppEnemy.KillCommandPost(cpID)`
- `TppEnemy.KillEnemy(characterID)`
- `TppEnemy.RealizeEnemy(cpID, characterID, options)`
- `TppEnemy.RegisterHoldTime(cpID, holdTime)`
- `TppEnemy.RegisterRouteSet(cpID, routeSetType, routeSetName)`
- `TppEnemy.SetEnemyStatus(characterID, status)`
- `TppEnemy.SetFormVariation(characterID, fovaKey)`
- `TppEnemy.SetMinimumPhase(cpID, phase)`
  - `phase` expects: "sneak", "caution", "alert"
- `TppEnemy.SetPosition(characterID, position)`
- `TppEnemy.SetRotation(characterID, rotation)`
- `TppEnemy.SetRouteSets(routeSets, options)`
- `TppEnemy.SetStartingRouteSet(cpID, options)`
- `TppEnemy.Setup()`
- `TppEnemy.ShiftRouteSet(cpID, routeSets, holdTime, options)`
- `TppEnemy.UnrealizeEnemy(cpID, characterID, options)`
- `TppEnemy.Warp(characterID, warpLocator)`
- `TppEnemy._ChangeRouteSetOnPhase(phase)`
  - `phase` expects: "sneak", "caution", "alert"
- `TppEnemy._ChangeRouteSetOnTime(time)`
- `TppEnemy._IsCharacterIDValid(characterID)`
- `TppEnemy._IsPhaseNameValid(phaseName)`
- `TppEnemy._IsRouteSetTypeValid(routeSetType)`
- `TppEnemy.commonFunc()`

## TppGimmick (class)
- `TppGimmick.CloseDoor(doorID)`
- `TppGimmick.OpenDoor(doorID, openAngle)`

## TppHelicopter (class)
- `TppHelicopter.Call(rvPointName)`
- `TppHelicopter.ChangeRVPoint(rvPointName)`
- `TppHelicopter.ChangeRoute(routeID)`
- `TppHelicopter.DisableAutoReturn()`
- `TppHelicopter.DisableHelicopter()`
- `TppHelicopter.EnableAutoReturn()`
- `TppHelicopter.EnableHelicopter()`
- `TppHelicopter.SetRoute(routeID, speed, options)`
- `TppHelicopter.SetStatus(statusName)`

## TppLocation (class)
- `TppLocation.AddClearAreaTrap(trapName)`
- `TppLocation.AddIntelSearchAreaTrap(trapName)`
- `TppLocation.DeleteClearAreaTrap(trapName)`
- `TppLocation.DeleteIntelSearchAreaTrap(trapName)`
- `TppLocation.GetCommandPostIDs()`
- `TppLocation.GetCommandPostNames()`
- `TppLocation.GetDefaultRouteSets()`
- `TppLocation.GetFlag(flagName)`
- `TppLocation.GetLocationName()`
- `TppLocation.GetOuterBaseIDs()`
- `TppLocation.GetOuterBaseNames()`
- `TppLocation.GetTimes()`
- `TppLocation.Register(script)`
- `TppLocation.SetFlag(flagName, value)`
- `TppLocation.Setup()`
- `TppLocation.onEnterClearAreaTrap()`
- `TppLocation.onEnterIntelSearchAreaTrap()`
- `TppLocation.onExitClearAreaTrap()`
- `TppLocation.onExitIntelSearchAreaTrap()`

## TppMarker (class)
- `TppMarker.Disable(markerID)`
- `TppMarker.DisableAll()`
- `TppMarker.Enable(markerID, visibleArea, goalType, viewType, randomRange, setImportant, setNew)`
- `TppMarker.Setup()`

## TppMission (class)
- `TppMission.ChangeState(state, stateMessage, flags)`
- `TppMission.EnableAcceptMission()`
- `TppMission.GetCurrentState()`
- `TppMission.GetFlag(flagID)`
- `TppMission.GetMissionID()`
- `TppMission.GetMissionName()`
- `TppMission.IsDemoBlockActive()`
- `TppMission.IsEventBlockActive()`
- `TppMission.LoadDemoBlock(path)`
- `TppMission.LoadEventBlock(path)`
- `TppMission.OnEnterClearAreaTrap()`
- `TppMission.OnExitClearAreaTrap()`
- `TppMission.OnLeaveInnerArea(doFunc)`
- `TppMission.OnLeaveOuterArea(doFunc)`
- `TppMission.Register(missionID, missionFlagList)`
- `TppMission.RegisterVipRestorePoint(characterId, keyName)`
- `TppMission.SetFlag(flagID, value)`
- `TppMission.SetInGame(inGame)`
- `TppMission.Setup()`
- `TppMission.Start()`
- `TppMission.StartEndingSequence(params)`
- `TppMission.UnregisterVipRestorePoint(characterId)`
- `TppMission._CreateMissionName(missionID)`
- `TppMission._execInnerCrossing()`
- `TppMission._execOuterCrossing()`
- `TppMission._onEndEndingSequence()`

## TppPlayer (class)
- `TppPlayer.AddWeapons(weapons)`
- `TppPlayer.DisableAbility(abilityName)`
  - `abilityName` expects: "Stand", "Squat", "Crawl", "Dash"
- `TppPlayer.DisableControlMode(controlMode)`
  - `controlMode` expects: "LockPadMode", "LockMBTerminalOpenCloseMode", "MBTerminalOnlyMode"
- `TppPlayer.EnableAbility(abilityName)`
  - `abilityName` expects: "Stand", "Squat", "Crawl", "Dash"
- `TppPlayer.EnableControlMode(controlMode)`
  - `controlMode` expects: "LockPadMode", "LockMBTerminalOpenCloseMode", "MBTerminalOnlyMode"
- `TppPlayer.GetPlayer()`
- `TppPlayer.GetPosition()`
- `TppPlayer.GetRotation()`
- `TppPlayer.SetDefaultFacialMotion(facialMotionName)`
- `TppPlayer.SetEventStatus(eventStatus)`
- `TppPlayer.SetFormVariation(fovaKey)`
- `TppPlayer.SetPosition(position)`
- `TppPlayer.SetRotation(rotation)`
- `TppPlayer.SetStartStatus(startStatus)`
  - `startStatus` expects: "rideHeli_sit", "rideHeli_standLeft", "rideHeli_standRight"
- `TppPlayer.SetWeapons(weapons)`
- `TppPlayer.Warp(warpLocator)`
- `TppPlayer.WarpForDebugStartInVipRestorePoint(manager, characterID, warpLocatorKey)`
- `TppPlayer._IsAbilityNameValid(abilityName)`
  - `abilityName` expects: "Stand", "Squat", "Crawl", "Dash"
- `TppPlayer._IsControlModeValid(controlMode)`
  - `controlMode` expects: "LockPadMode", "LockMBTerminalOpenCloseMode", "MBTerminalOnlyMode"
- `TppPlayer._IsStartStatusValid(startStatus)`
  - `startStatus` expects: "rideHeli_sit", "rideHeli_standLeft", "rideHeli_standRight"

## TppRadio (class)
- `TppRadio.DelayPlay(radioTag, delayTime, noiseType, funcs, radioType)`
- `TppRadio.DelayPlayEnqueue(radioTag, delayTime, noiseType, funcs, radioType)`
- `TppRadio.DeleteOptionalRadio(optionalRadioID, radioData)`
- `TppRadio.DisableIntelRadio(locatorID)`
- `TppRadio.DisableIntelRadioCharacterType(characterType)`
- `TppRadio.EnableIntelRadio(locatorID)`
- `TppRadio.EnableIntelRadioCharacterType(characterType)`
- `TppRadio.IdRadioPlayable()`
- `TppRadio.InsertOptionalRadio(optionalRadioID, radioData, index)`
- `TppRadio.OnRadioEnd()`
- `TppRadio.OnRadioPlay()`
- `TppRadio.OnRadioRequest()`
- `TppRadio.Play(radioTag, funcs, radioType, preDelayTimeAtSec, noiseType)`
- `TppRadio.PlayDebug(radioTag, funcs)`
- `TppRadio.PlayEnqueue(radioTag, funcs, radioType, preDelayTimeAtSec, noiseType)`
- `TppRadio.PlayStrong(radioTag, funcs, radioType, preDelayTimeAtSec, noiseType)`
- `TppRadio.Register(radioList, optionalRadioList, intelRadioList)`
- `TppRadio.RegisterIntelRadio(intelRadioID, radioData, isSave)`
- `TppRadio.RegisterOptionalRadio(optionalRadioID)`
- `TppRadio.ResetEpisonageRadioSubPriority(optionalRadioID, radioData, index)`
- `TppRadio.ResetOptionalRadioSubPriority(optionalRadioID, radioData, index)`
- `TppRadio.ResetRealTimeRadioSubPriority(optionalRadioID, radioData, index)`
- `TppRadio.RestoreIntelRadio()`
- `TppRadio.SetAllSaveRadioId()`
- `TppRadio.Start()`
- `TppRadio._DoMessage(radioName, funcName)`
- `TppRadio._DoPlayRadio(radioName, radioIDs, numPlay, radioType)`
- `TppRadio._GetDataFromName(radioName, radioType)`
- `TppRadio._GetDataFromTable(radioGroup, radioType)`
- `TppRadio._GetRadioIDsAndNumPlay(radioTag, radioType)`
- `TppRadio._GetRadioName(radioIDs)`
- `TppRadio._IsRadioLocatorIDValid(radioLocatorID)`
- `TppRadio._MessageHandler(manager, data, radioName, message)`
- `TppRadio._OnRadioEnd(radioName)`
- `TppRadio._OnRadioPlay(radioName)`
- `TppRadio._OnRadioRequest(radioName)`
- `TppRadio._PlayDebugContinue()`
- `TppRadio._PlayDebugLine(text, time)`
- `TppRadio._PlayDebugStart()`
- `TppRadio._PlayIntelRadio()`
- `TppRadio.onIntelRadioEnd()`
- `TppRadio.onIntelRadioStart()`

## TppSequence (class)
- `TppSequence.ChangeMissionState(missionManager, state, stateMessage, flags)`
- `TppSequence.ChangeSequence(seqName, esmType)`
- `TppSequence.GetCurrentSequence(esmType)`
- `TppSequence.GetData(keyName, esmType)`
- `TppSequence.GetManager(esmType)`
- `TppSequence.IsCurrentSequenceAtoB(seq1, seq2, esmType)`
- `TppSequence.IsExist(name)`
- `TppSequence.IsGreaterThan(seq1, seq2, esmType)`
- `TppSequence.IsInSequences(name)`
- `TppSequence.Register(script, manager, esmType)`
- `TppSequence._GetScript(esmType)`
- `TppSequence._IsSequenceValid(seqName, esmType)`
- `TppSequence._IsTypeValid(esmType)`

## TppSound (class)
- `TppSound.PlayBGM(bgmName)`
- `TppSound.PlayEvent(eventName)`
- `TppSound.RegisterSourceEvent(name, tag, playEvent, stopEvent)`
- `TppSound.Start()`
- `TppSound.StopBGM(bgmName)`
- `TppSound.UnregisterSourceEvent(name, tag)`

## TppStaticModel (class)
- `TppStaticModel.Hide(staticModelData)`
- `TppStaticModel.Show(staticModelData)`
- `TppStaticModel._IsStaticModel(staticModelData)`

## TppTerminal (class)
- `TppTerminal.ActivateMenu(menuID)`
  - `menuID` expects: "MotherBase", "MissionList", "DataBase", "MotherBase_Staff", "MotherBase_Develop", "MotherBase_CombatDeployment", "MotherBase_Support", "MotherBase_Security", "MotherBase_Union", "DataBase_PlayerData", "DataBase_MotherBaseData", "DataBase_Information", "MotherBase_Develop_Weapon", "MotherBase_Develop_SupportWeapon", "MotherBase_Develop_Bullet", "MotherBase_Develop_Item", "MotherBase_Develop_Suits", "MotherBase_Develop_Mecha", "MotherBase_Develop_Plant", "MotherBase_Support_Goods", "MotherBase_Support_Weapon", "MotherBase_Support_SupportWeapon", "MotherBase_Support_Vehicle", "MotherBase_Support_Unmanned", "MotherBase_Union_Rental", "Strike", "Order", "Strike_ArtilleryRequest", "Strike_SmokeRequest", "Strike_SupplyRequest", "Strike_VehicleRequest", "Strike_JammingRequest", "Order_Helicopter", "Order_Quiet", "Order_DDog"
- `TppTerminal.AddStaff(staffNum, unitID)`
  - `unitID` expects: "All", "Unit_Combat", "Unit_Develop", "Unit_Intel", "Unit_Medical", "Unit_Support", "Unit_MBDevelop", "Unit_Security", "Room_Waiting", "Room_Hospital", "Room_Isolation", "Room_Jail", "Room_Trade"
- `TppTerminal.DeactivateMenu(menuID)`
  - `menuID` expects: "MotherBase", "MissionList", "DataBase", "MotherBase_Staff", "MotherBase_Develop", "MotherBase_CombatDeployment", "MotherBase_Support", "MotherBase_Security", "MotherBase_Union", "DataBase_PlayerData", "DataBase_MotherBaseData", "DataBase_Information", "MotherBase_Develop_Weapon", "MotherBase_Develop_SupportWeapon", "MotherBase_Develop_Bullet", "MotherBase_Develop_Item", "MotherBase_Develop_Suits", "MotherBase_Develop_Mecha", "MotherBase_Develop_Plant", "MotherBase_Support_Goods", "MotherBase_Support_Weapon", "MotherBase_Support_SupportWeapon", "MotherBase_Support_Vehicle", "MotherBase_Support_Unmanned", "MotherBase_Union_Rental", "Strike", "Order", "Strike_ArtilleryRequest", "Strike_SmokeRequest", "Strike_SupplyRequest", "Strike_VehicleRequest", "Strike_JammingRequest", "Order_Helicopter", "Order_Quiet", "Order_DDog"
- `TppTerminal.DeleteAllStaff()`
- `TppTerminal.DisableControlMode(controlMode)`
  - `controlMode` expects: "LockCloseByCancellingMode"
- `TppTerminal.DisableDevelopWeapon(weaponID)`
- `TppTerminal.DisableUnit(unitID)`
  - `unitID` expects: "All", "Unit_Combat", "Unit_Develop", "Unit_Intel", "Unit_Medical", "Unit_Support", "Unit_MBDevelop", "Unit_Security", "Room_Waiting", "Room_Hospital", "Room_Isolation", "Room_Jail", "Room_Trade"
- `TppTerminal.EnableControlMode(controlMode)`
  - `controlMode` expects: "LockCloseByCancellingMode"
- `TppTerminal.EnableDevelopWeapon(weaponID)`
- `TppTerminal.EnableUnit(unitID)`
  - `unitID` expects: "All", "Unit_Combat", "Unit_Develop", "Unit_Intel", "Unit_Medical", "Unit_Support", "Unit_MBDevelop", "Unit_Security", "Room_Waiting", "Room_Hospital", "Room_Isolation", "Room_Jail", "Room_Trade"
- `TppTerminal.HideMenu(menuID)`
  - `menuID` expects: "MotherBase", "MissionList", "DataBase", "MotherBase_Staff", "MotherBase_Develop", "MotherBase_CombatDeployment", "MotherBase_Support", "MotherBase_Security", "MotherBase_Union", "DataBase_PlayerData", "DataBase_MotherBaseData", "DataBase_Information", "MotherBase_Develop_Weapon", "MotherBase_Develop_SupportWeapon", "MotherBase_Develop_Bullet", "MotherBase_Develop_Item", "MotherBase_Develop_Suits", "MotherBase_Develop_Mecha", "MotherBase_Develop_Plant", "MotherBase_Support_Goods", "MotherBase_Support_Weapon", "MotherBase_Support_SupportWeapon", "MotherBase_Support_Vehicle", "MotherBase_Support_Unmanned", "MotherBase_Union_Rental", "Strike", "Order", "Strike_ArtilleryRequest", "Strike_SmokeRequest", "Strike_SupplyRequest", "Strike_VehicleRequest", "Strike_JammingRequest", "Order_Helicopter", "Order_Quiet", "Order_DDog"
- `TppTerminal.Setup()`
- `TppTerminal.ShowMenu(menuID)`
  - `menuID` expects: "MotherBase", "MissionList", "DataBase", "MotherBase_Staff", "MotherBase_Develop", "MotherBase_CombatDeployment", "MotherBase_Support", "MotherBase_Security", "MotherBase_Union", "DataBase_PlayerData", "DataBase_MotherBaseData", "DataBase_Information", "MotherBase_Develop_Weapon", "MotherBase_Develop_SupportWeapon", "MotherBase_Develop_Bullet", "MotherBase_Develop_Item", "MotherBase_Develop_Suits", "MotherBase_Develop_Mecha", "MotherBase_Develop_Plant", "MotherBase_Support_Goods", "MotherBase_Support_Weapon", "MotherBase_Support_SupportWeapon", "MotherBase_Support_Vehicle", "MotherBase_Support_Unmanned", "MotherBase_Union_Rental", "Strike", "Order", "Strike_ArtilleryRequest", "Strike_SmokeRequest", "Strike_SupplyRequest", "Strike_VehicleRequest", "Strike_JammingRequest", "Order_Helicopter", "Order_Quiet", "Order_DDog"
- `TppTerminal.Start()`
- `TppTerminal.StartIntelEnemySearch(cpID)`
- `TppTerminal.StopIntelEnemySearch()`
- `TppTerminal._DoControlMode(controlMode, action, isDo)`
  - `controlMode` expects: "LockCloseByCancellingMode"
- `TppTerminal._DoDevelopWeapon(weaponID, action, isDo)`
- `TppTerminal._DoMenu(menuID, action, isDo)`
  - `menuID` expects: "MotherBase", "MissionList", "DataBase", "MotherBase_Staff", "MotherBase_Develop", "MotherBase_CombatDeployment", "MotherBase_Support", "MotherBase_Security", "MotherBase_Union", "DataBase_PlayerData", "DataBase_MotherBaseData", "DataBase_Information", "MotherBase_Develop_Weapon", "MotherBase_Develop_SupportWeapon", "MotherBase_Develop_Bullet", "MotherBase_Develop_Item", "MotherBase_Develop_Suits", "MotherBase_Develop_Mecha", "MotherBase_Develop_Plant", "MotherBase_Support_Goods", "MotherBase_Support_Weapon", "MotherBase_Support_SupportWeapon", "MotherBase_Support_Vehicle", "MotherBase_Support_Unmanned", "MotherBase_Union_Rental", "Strike", "Order", "Strike_ArtilleryRequest", "Strike_SmokeRequest", "Strike_SupplyRequest", "Strike_VehicleRequest", "Strike_JammingRequest", "Order_Helicopter", "Order_Quiet", "Order_DDog"
- `TppTerminal._DoUnit(unitID, action, isDo)`
  - `unitID` expects: "All", "Unit_Combat", "Unit_Develop", "Unit_Intel", "Unit_Medical", "Unit_Support", "Unit_MBDevelop", "Unit_Security", "Room_Waiting", "Room_Hospital", "Room_Isolation", "Room_Jail", "Room_Trade"
- `TppTerminal._IsValid(checkTable, checkID)`

## TppTimer (class)
- `TppTimer.IsTimerActive(timerName)`
- `TppTimer.Setup()`
- `TppTimer.Start(timerName, timerTime)`
- `TppTimer.Stop(timerName)`
- `TppTimer.StopAll()`

## TppUI (class)
- `TppUI.DisableHUD()`
- `TppUI.DisableTerminal()`
- `TppUI.EnableHUD()`
- `TppUI.EnableTerminal()`
- `TppUI.FadeIn(frameNum)`
- `TppUI.FadeOut(frameNum)`
- `TppUI.OnEndingStartNextLoading()`
- `TppUI.OnEndingTransitionEnd()`
- `TppUI.OnEndingTransitionStart()`
- `TppUI.OnOpeningTransitionBgEnd()`
- `TppUI.OnOpeningTransitionEnd()`
- `TppUI.OnOpeningTransitionStart()`
- `TppUI.ShowAllMarkers()`
- `TppUI.ShowIcon(tutorialName, iconName)`
- `TppUI.ShowTransition(transitionType, funcs)`
- `TppUI.ShowTransitionInGame(transitionType, funcs)`
- `TppUI.ShowTransitionWithFadeIn(transitionType, funcs, fadeInSec)`
- `TppUI.ShowTransitionWithFadeOut(transitionType, funcs, fadeOutSec)`
- `TppUI.ShowTransitionWithFadeOutIn(transitionType, funcs, fadeOutSec, fadeInSec)`
- `TppUI.Start()`
- `TppUI._DisableGameStatusOnFade()`
- `TppUI._DoMessage(transitionName, funcName)`
- `TppUI._EnableGameStatusOnFade()`
- `TppUI._OnEndingStartNextLoading()`
- `TppUI._OnEndingTransitionEnd()`
- `TppUI._OnEndingTransitionFadeInEnd()`
- `TppUI._OnEndingTransitionFadeOutEnd()`
- `TppUI._OnEndingTransitionRadioStop()`
- `TppUI._OnEndingTransitionStart()`
- `TppUI._OnOpeningTransitionBgEnd()`
- `TppUI._OnOpeningTransitionEnd()`
- `TppUI._OnOpeningTransitionFadeInEnd()`
- `TppUI._OnOpeningTransitionFadeOutEnd()`
- `TppUI._OnOpeningTransitionStart()`
- `TppUI._PlayBGM(transitionType, bgmName)`
- `TppUI._ShowTransition(transitionType, inGame, funcs, fadeInSec)`

## TppUtility (class)
- `TppUtility.FindDistance(point1, point2)`
- `TppUtility.GetValueIndex(tbl, value)`
- `TppUtility.InvertTable(tbl)`
- `TppUtility.IsIncluded(tbl, item)`
- `TppUtility.SetPreference(prefName, propName, propValue)`
- `TppUtility.SplitString(str, pat)`

## TppWeather (class)
- `TppWeather.SetChangeTime(changeTime)`
- `TppWeather.SetDefaultWeather(weather)`
  - `weather` expects: "sunny", "cloudy", "rainy", "sandstorm", "foggy", "pouring"
- `TppWeather.SetProbabilities(probabilities)`
- `TppWeather.SetRandomness(changeInterval, changeIntervalAdjustment, changeTime)`
- `TppWeather.SetWeather(weather, conversionTime)`
  - `weather` expects: "sunny", "cloudy", "rainy", "sandstorm", "foggy", "pouring"
- `TppWeather.Start()`
- `TppWeather.Stop()`
- `TppWeather._IsWeatherNameValid(weatherName)`
  - `weatherName` expects: "sunny", "cloudy", "rainy", "sandstorm", "foggy", "pouring"

## TppLocationSettings (class)
- `TppLocationSettings.GetRouteSets(locationName)`
- `TppLocationSettings.GetTimes(locationName)`

