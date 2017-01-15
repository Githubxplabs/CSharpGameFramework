﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameFramework.Skill;
using GameFrameworkMessage;
using ScriptRuntime;
using SkillSystem;

namespace GameFramework
{
    public class EntityController
    {
        public void Init(Scene scene, EntityManager mgr)
        {
            m_Scene = scene;
            m_EntityMgr = mgr;
            mgr.OnDamage += OnDamage;
        }

        public void Reset()
        {

        }

        public void Release()
        {
        }

        public void Tick()
        {
        }
        
        public bool ExistGameObject(int objId)
        {
            EntityInfo obj = m_Scene.EntityManager.GetEntityInfo(objId);
            return null != obj;
        }
        public EntityInfo GetGameObject(int objId)
        {
            EntityInfo obj = m_Scene.EntityManager.GetEntityInfo(objId);
            return obj;
        }
        public int GetEntityType(int objId)
        {
            int type = 0;
            EntityInfo obj = m_Scene.EntityManager.GetEntityInfo(objId);
            if (null != obj) {
                type = obj.EntityType;
            }
            return type;
        }
        public int GetEntityType(EntityInfo obj)
        {
            int type = 0;
            if (null != obj) {
                type = obj.EntityType;
            }
            return type;
        }
        public int GetCampId(int objId)
        {
            int campId = 0;
            EntityInfo entity = m_Scene.EntityManager.GetEntityInfo(objId);
            if (null != entity) {
                campId = entity.GetCampId();
            }
            return campId;
        }
        public int GetCampId(EntityInfo obj)
        {
            int campId = 0;
            if (null != obj) {
                campId = obj.GetCampId();
            }
            return campId;
        }
        public bool CanCastSkill(int objId, TableConfig.Skill configData, int seq)
        {
            bool ret=true;
            if (configData.type == (int)SkillOrImpactType.Skill) {
                EntityInfo entity = m_Scene.EntityManager.GetEntityInfo(objId);
                if (null != entity) {
                    if (entity.GetSkillStateInfo().IsSkillActivated()) {
                        SkillInfo skillInfo = entity.GetSkillStateInfo().GetCurSkillInfo();
                        if (null != skillInfo && skillInfo.ConfigData.skillData.interruptPriority >= configData.skillData.interruptPriority) {
                            ret = false;
                        }
                    }
                }
            }
            return ret;
        }
        public void CancelCastSkill(int objId)
        {
            EntityInfo entity = m_Scene.EntityManager.GetEntityInfo(objId);
            if (null != entity) {
                entity.IsControlByManual = false;
            }
        }
        public GfxSkillSenderInfo BuildSkillInfo(int objId, TableConfig.Skill configData, int seq, Scene scene)
        {
            GfxSkillSenderInfo ret = null;
            EntityInfo entity = m_Scene.EntityManager.GetEntityInfo(objId);
            if (null != entity && null != configData) {
                int targetId = 0;
                if (configData.type == (int)SkillOrImpactType.Skill) {
                    targetId = entity.GetAiStateInfo().Target;
                } else {
                    ImpactInfo impactInfo = entity.GetSkillStateInfo().GetImpactInfoBySeq(seq);
                    if (null != impactInfo) {
                        targetId = impactInfo.ImpactSenderId;
                    }
                }
                EntityInfo targetObj = m_Scene.EntityManager.GetEntityInfo(targetId);
                if (null != targetObj) {
                    ret = new GfxSkillSenderInfo(configData, seq, objId, entity, targetId, targetObj, scene);
                } else {
                    ret = new GfxSkillSenderInfo(configData, seq, objId, entity, scene);
                }
            }
            return ret;
        }
        public void ActivateSkill(int objId, int skillId, int seq)
        {
            EntityInfo entity = m_Scene.EntityManager.GetEntityInfo(objId);
            if (null != entity) {
                SkillInfo skillInfo = entity.GetSkillStateInfo().GetSkillInfoById(skillId);
                if (null != skillInfo) {
                    SkillInfo curSkillInfo = entity.GetSkillStateInfo().GetCurSkillInfo();
                    if (null != curSkillInfo && (curSkillInfo.ConfigData.skillData.interruptPriority < skillInfo.ConfigData.skillData.interruptPriority)) {
                        m_Scene.SkillSystem.StopSkill(objId, curSkillInfo.SkillId, 0, true);
                        if (skillId == entity.ManualSkillId) {
                            LogSystem.Warn("ManualSkill {0} interrupt {1}.", skillId, curSkillInfo.SkillId);
                        }
                    }
                    if (skillId == entity.ManualSkillId) {
                        LogSystem.Warn("ManualSkill {0} activate.", skillId);
                    }
                    entity.GetSkillStateInfo().SetCurSkillInfo(skillId);
                    skillInfo.IsSkillActivated = true;
                    skillInfo.CdEndTime = TimeUtility.GetLocalMilliseconds() + (long)skillInfo.ConfigData.skillData.cooldown;
                    if (skillInfo.ConfigData.skillData.addsc > 0) {
                        //回蓝
                        entity.Energy += skillInfo.ConfigData.skillData.addsc;
                        entity.EntityManager.FireDamageEvent(objId, 0, false, false, 0, -skillInfo.ConfigData.skillData.addsc);
                    }
                }
            }
        }
        public void DeactivateSkill(int objId, int skillId, int seq)
        {
            EntityInfo entity = m_Scene.EntityManager.GetEntityInfo(objId);
            if (null != entity) {
                SkillInfo skillInfo = entity.GetSkillStateInfo().GetSkillInfoById(skillId);
                if (null != skillInfo) {
                    if (skillId == entity.ManualSkillId) {
                        LogSystem.Warn("ManualSkill {0} deactivate.", skillId);
                    }
                    skillInfo.IsSkillActivated = false;
                    entity.IsControlByManual = false;
                } else {
                    entity.GetSkillStateInfo().RemoveImpact(seq);
                }
            }
        }
        public void CancelIfImpact(int actorId, TableConfig.Skill cfg, int seq)
        {
            EntityInfo entity = m_Scene.EntityManager.GetEntityInfo(actorId);
            if (null != entity) {
                if (cfg.type != (int)SkillOrImpactType.Skill) {
                    entity.GetSkillStateInfo().RemoveImpact(seq);
                }
            }
        }
        public int SelectTargetForSkill(string type, int objId, TableConfig.Skill cfg, int seq, HashSet<int> history)
        {
            if (string.IsNullOrEmpty(type))
                return 0;
            EntityInfo entity = m_Scene.EntityManager.GetEntityInfo(objId);
            if (null != entity) {
                int campId = entity.GetCampId();
                if (type.CompareTo("minhp") == 0) {
                    int targetId = 0;
                    float minhp = float.MaxValue;
                    ScriptRuntime.Vector3 pos = entity.GetMovementStateInfo().GetPosition3D();
                    m_Scene.KdTree.QueryWithAction(pos, cfg.skillData.distance, (float distSqr, KdTreeObject kdObj) => {
                        if (minhp > kdObj.Object.Hp && EntityInfo.GetRelation(campId, kdObj.Object.GetCampId()) == CharacterRelation.RELATION_ENEMY) {
                            int id = kdObj.Object.GetId();
                            if (!history.Contains(id)) {
                                minhp = kdObj.Object.Hp;
                                targetId = id;
                            }
                        }
                    });
                    return targetId;
                } else if (type.CompareTo("maxdist") == 0) {
                    int targetId = 0;
                    float maxDistSqr = 0;
                    ScriptRuntime.Vector3 pos = entity.GetMovementStateInfo().GetPosition3D();
                    m_Scene.KdTree.QueryWithAction(pos, cfg.skillData.distance, (float distSqr, KdTreeObject kdObj) => {
                        if (maxDistSqr < distSqr && EntityInfo.GetRelation(campId, kdObj.Object.GetCampId()) == CharacterRelation.RELATION_ENEMY) {
                            int id = kdObj.Object.GetId();
                            if (!history.Contains(id)) {
                                maxDistSqr = distSqr;
                                targetId = id;
                            }
                        }
                    });
                    return targetId;
                } else if (type.CompareTo("randenemy") == 0) {
                    return GetRandEnemyId(campId, history);
                } else if (type.CompareTo("randfriend") == 0) {
                    return GetRandFriendId(campId, history);
                }
            }
            return 0;
        }
        public int GetRandEnemyId(int campId, HashSet<int> history)
        {
            int id = 0;
            List<int> ids = new List<int>();
            for (LinkedListNode<EntityInfo> linkNode = m_Scene.EntityManager.Entities.FirstValue; null != linkNode; linkNode = linkNode.Next) {
                EntityInfo info = linkNode.Value;
                if (EntityInfo.GetRelation(campId, info.GetCampId()) == CharacterRelation.RELATION_ENEMY && info.EntityType != (int)EntityTypeEnum.Tower) {
                    ids.Add(info.GetId());
                }
            }
            for (int ct = 0; ct < ids.Count; ++ct) {
                int index = Helper.Random.Next(ids.Count);
                if (index >= 0 && index < ids.Count) {
                    int _id = ids[index];
                    if (!history.Contains(id)) {
                        id = _id;
                        break;
                    }
                }
            }
            return id;
        }
        public int GetRandFriendId(int campId, HashSet<int> history)
        {
            int id = 0;
            List<int> ids = new List<int>();
            for (LinkedListNode<EntityInfo> linkNode = m_Scene.EntityManager.Entities.FirstValue; null != linkNode; linkNode = linkNode.Next) {
                EntityInfo info = linkNode.Value;
                if (EntityInfo.GetRelation(campId, info.GetCampId()) == CharacterRelation.RELATION_FRIEND && info.EntityType != (int)EntityTypeEnum.Tower) {
                    ids.Add(info.GetId());
                }
            }
            for (int ct = 0; ct < ids.Count; ++ct) {
                int index = Helper.Random.Next(ids.Count);
                if (index >= 0 && index < ids.Count) {
                    int _id = ids[index];
                    if (!history.Contains(id)) {
                        id = _id;
                        break;
                    }
                }
            }
            return id;
        }
        public int GetRandEnemyId(int campId)
        {
            int id = 0;
            List<int> ids = new List<int>();
            for (LinkedListNode<EntityInfo> linkNode = m_Scene.EntityManager.Entities.FirstValue; null != linkNode; linkNode = linkNode.Next) {
                EntityInfo info = linkNode.Value;
                if (EntityInfo.GetRelation(campId, info.GetCampId()) == CharacterRelation.RELATION_ENEMY && info.EntityType != (int)EntityTypeEnum.Tower) {
                    ids.Add(info.GetId());
                }
            }
            int index = Helper.Random.Next(ids.Count);
            if (index >= 0 && index < ids.Count) {
                id = ids[index];
            }
            return id;
        }
        public int GetRandFriendId(int campId)
        {
            int id = 0;
            List<int> ids = new List<int>();
            for (LinkedListNode<EntityInfo> linkNode = m_Scene.EntityManager.Entities.FirstValue; null != linkNode; linkNode = linkNode.Next) {
                EntityInfo info = linkNode.Value;
                if (EntityInfo.GetRelation(campId, info.GetCampId()) == CharacterRelation.RELATION_FRIEND && info.EntityType != (int)EntityTypeEnum.Tower) {
                    ids.Add(info.GetId());
                }
            }
            int index = Helper.Random.Next(ids.Count);
            if (index >= 0 && index < ids.Count) {
                id = ids[index];
            }
            return id;
        }
        public bool HaveShield(int objId)
        {
            EntityInfo entity = m_Scene.EntityManager.GetEntityInfo(objId);
            if (null != entity) {
                if (entity.Shield > 0) {
                    return true;
                }
            }
            return false;
        }
        public int GetTargetType(int objId, TableConfig.Skill cfg, int seq)
        {
            if (cfg.type == (int)SkillOrImpactType.Skill) {
                return cfg.targetType;
            } else {
                EntityInfo entity = m_Scene.EntityManager.GetEntityInfo(objId);
                if (null != entity) {
                    ImpactInfo impactInfo = entity.GetSkillStateInfo().GetImpactInfoBySeq(seq);
                    if (null != impactInfo) {
                        return impactInfo.TargetType;
                    }
                }
            }
            return 0;
        }
        public int GetImpactDuration(int objId, int impactId, int seq)
        {
            EntityInfo entity = m_Scene.EntityManager.GetEntityInfo(objId);
            if (null != entity) {
                ImpactInfo impactInfo = entity.GetSkillStateInfo().GetImpactInfoBySeq(seq);
                if (null != impactInfo && impactId == impactInfo.ImpactId) {
                    return impactInfo.DurationTime;
                }
            }
            return 0;
        }
        public Vector3 GetImpactSenderPosition(int objId, int impactId, int seq)
        {
            EntityInfo entity = m_Scene.EntityManager.GetEntityInfo(objId);
            if (null != entity) {
                ImpactInfo impactInfo = entity.GetSkillStateInfo().GetImpactInfoBySeq(seq);
                if (null != impactInfo && impactId == impactInfo.ImpactId) {
                    return new Vector3(impactInfo.SenderPosition.X, impactInfo.SenderPosition.Y, impactInfo.SenderPosition.Z);
                }
            }
            return Vector3.Zero;
        }
        public int GetImpactSkillId(int actorId, int impactId, int seq)
        {
            EntityInfo entity = m_Scene.EntityManager.GetEntityInfo(actorId);
            if (null != entity) {
                ImpactInfo impactInfo = entity.GetSkillStateInfo().GetImpactInfoBySeq(seq);
                if (null != impactInfo && impactId == impactInfo.ImpactId) {
                    return impactInfo.SkillId;
                }
            }
            return 0;
        }
        public void CalcSenderAndTarget(GfxSkillSenderInfo senderObj, out int senderId, out int targetId)
        {
            senderId = 0;
            targetId = 0;

            int targetType = GetTargetType(senderObj.ActorId, senderObj.ConfigData, senderObj.Seq);            
            if (senderObj.ConfigData.type == (int)SkillOrImpactType.Skill) {
                senderId = senderObj.ActorId;
                targetId = senderObj.TargetActorId;
                if (targetType == (int)SkillTargetType.RandEnemy) {
                    targetId = GetRandEnemyId(GetCampId(senderId));
                } else if (targetType == (int)SkillTargetType.RandFriend) {
                    targetId = GetRandFriendId(GetCampId(senderId));
                } else if (targetType == (int)SkillTargetType.Friend) {
                    targetId = senderObj.ActorId;
                } else if (targetType == (int)SkillTargetType.Self) {
                    targetId = senderObj.ActorId;
                }
            } else {
                senderId = senderObj.TargetActorId;
                targetId = senderObj.ActorId;
                if (targetType == (int)SkillTargetType.RandEnemy) {
                    targetId = GetRandEnemyId(GetCampId(senderId));
                } else if (targetType == (int)SkillTargetType.RandFriend) {
                    targetId = GetRandFriendId(GetCampId(senderId));
                } else if (targetType == (int)SkillTargetType.Friend) {
                    targetId = senderObj.TargetActorId;
                } else if (targetType == (int)SkillTargetType.Self) {
                    targetId = senderObj.TargetActorId;
                }
            }
        }
        public CharacterRelation GetRelation(int one, int other)
        {
            EntityInfo entity1 = m_Scene.EntityManager.GetEntityInfo(one);
            EntityInfo entity2 = m_Scene.EntityManager.GetEntityInfo(other);
            if (null == entity1 || null == entity2)
                return CharacterRelation.RELATION_INVALID;
            else
                return EntityInfo.GetRelation(entity1, entity2);
        }
        public CharacterRelation GetRelation(EntityInfo one, EntityInfo other)
        {
            if (null == one || null == other)
                return CharacterRelation.RELATION_INVALID;
            else
                return EntityInfo.GetRelation(one, other);
        }
        public bool HaveState(int objId, string state)
        {
            EntityInfo entity = m_Scene.EntityManager.GetEntityInfo(objId);
            if (null != entity) {
                return entity.HaveState((CharacterPropertyEnum)CharacterStateUtility.NameToState(state));
            }
            return false;
        }
        public void AddState(int objId, string state)
        {
            EntityInfo entity = m_Scene.EntityManager.GetEntityInfo(objId);
            if (null != entity) {
                entity.AddState((CharacterPropertyEnum)CharacterStateUtility.NameToState(state));
            }
        }
        public void RemoveState(int objId, string state)
        {
            EntityInfo entity = m_Scene.EntityManager.GetEntityInfo(objId);
            if (null != entity) {
                if (string.IsNullOrEmpty(state)) {
                    //entity.StateFlag = 0;
                } else {
                    entity.RemoveState((CharacterPropertyEnum)CharacterStateUtility.NameToState(state));
                }
            }
        }
        public void AddShield(int objId, TableConfig.Skill cfg, int seq)
        {
            EntityInfo entity = m_Scene.EntityManager.GetEntityInfo(objId);
            if (null != entity) {
                int v;
                if (cfg.attrValues.TryGetValue((int)CharacterPropertyEnum.x2012_护盾值, out v)) {
                    entity.Shield += v;
                }
            }
        }
        public void RemoveShield(int objId, TableConfig.Skill cfg, int seq)
        {
            EntityInfo entity = m_Scene.EntityManager.GetEntityInfo(objId);
            if (null != entity) {
                entity.Shield = 0;
            }
        }
        public ImpactInfo SendImpact(int srcObjId, int targetId, int impactId, int skillId)
        {
            EntityInfo entity = m_Scene.EntityManager.GetEntityInfo(srcObjId);
            if(null!=entity){
                SkillInfo skillInfo = entity.GetSkillStateInfo().GetSkillInfoById(skillId);
                GfxSkillSenderInfo senderInfo;
                SkillInstance skillInst = m_Scene.SkillSystem.FindActiveSkillInstance(srcObjId,skillId,0,out senderInfo);
                if(null!=skillInst){
                    Dictionary<string, object> args;                    
                    Skill.Trigers.TriggerUtil.CalcImpactConfig(0, impactId, skillInst, senderInfo.ConfigData, out args);
                    return SendImpact(senderInfo.ConfigData, senderInfo.Seq, senderInfo.ActorId, srcObjId, targetId, impactId, args);
                }
            }
            return null;
        }
        public ImpactInfo SendImpact(TableConfig.Skill cfg, int seq, int curObjId, int srcObjId, int targetId, int impactId, Dictionary<string, object> args)
        {
            EntityInfo targetObj = m_Scene.EntityManager.GetEntityInfo(targetId);
            if (null != targetObj) {
                if (null != cfg) {
                    Quaternion hitEffectRotation = Quaternion.Identity;
                    EntityInfo srcObj = m_Scene.EntityManager.GetEntityInfo(srcObjId);
                    var addArgs = new Dictionary<string, object> { { "hitEffectRotation", hitEffectRotation } };
                    ImpactInfo impactInfo = null;
                    if (impactId <= 0 || impactId >= SkillInstance.c_FirstInnerHitSkillId) {
                        impactInfo = new ImpactInfo(m_Scene.SkillSystem.PredefinedSkill.HitSkillCfg);
                        impactId = PredefinedSkill.c_HitSkillId;
                    } else {
                        impactInfo = new ImpactInfo(impactId);
                    }
                    if (null != impactInfo.ConfigData) {
                        if (TryInitImpactInfo(impactInfo, cfg, seq, curObjId, srcObjId, args)) {
                            if (impactInfo.ConfigData.type == (int)SkillOrImpactType.Buff) {
                                ImpactInfo oldImpactInfo = targetObj.GetSkillStateInfo().FindImpactInfoById(impactInfo.ImpactId);
                                if (null != oldImpactInfo) {
                                    oldImpactInfo.DurationTime += impactInfo.DurationTime;
                                    return oldImpactInfo;
                                }
                            }
                        targetObj.GetSkillStateInfo().AddImpact(impactInfo);
                        SkillInfo skillInfo = targetObj.GetSkillStateInfo().GetCurSkillInfo();
                        if (null != skillInfo && cfg.skillData.isInterrupt) {
                            m_Scene.SkillSystem.StopSkill(targetId, skillInfo.SkillId, 0, true);
                        }
                        m_Scene.SkillSystem.StartSkill(targetId, impactInfo.ConfigData, impactInfo.Seq, args, addArgs);
                        return impactInfo;
                        }
                    } else {
                        LogSystem.Error("impact {0} config can't found !", impactInfo.ImpactId);
                    }
                }
            }
            return null;
        }
        public ImpactInfo TrackImpact(TableConfig.Skill cfg, int seq, int curObjId, int srcObjId, int targetId, string emitBone, int emitImpact, Vector3 offset, Dictionary<string, object> args)
        {
            EntityInfo targetObj = m_Scene.EntityManager.GetEntityInfo(targetId);
            EntityInfo srcObj = m_Scene.EntityManager.GetEntityInfo(srcObjId);
            if (null != targetObj && !targetObj.IsDead()) {
                if (null != cfg) {
                    ImpactInfo impactInfo = null;
                    if (emitImpact <= 0 || emitImpact >= SkillInstance.c_FirstInnerEmitSkillId) {
                        impactInfo = new ImpactInfo(m_Scene.SkillSystem.PredefinedSkill.EmitSkillCfg);
                    } else {
                        impactInfo = new ImpactInfo(emitImpact);
                    }
                    if (TryInitImpactInfo(impactInfo, cfg, seq, curObjId, srcObjId, args)) {
                        if (null != srcObj) {
                            Vector3 pos = srcObj.GetMovementStateInfo().GetPosition3D();
                            pos.Y += srcObj.GetRadius();
                            impactInfo.SenderPosition = pos;
                        }
                        targetObj.GetSkillStateInfo().AddImpact(impactInfo);
                        m_Scene.SkillSystem.StartSkill(targetId, impactInfo.ConfigData, impactInfo.Seq, args);
                        return impactInfo;
                    }
                }
            }
            return null;
        }
        public int GetTrackSendImpact(int targetId, int seq, Dictionary<string, object> args)
        {
            int impactId = 0;
            EntityInfo npc = m_Scene.EntityManager.GetEntityInfo(targetId);
            if (null != npc) {
                ImpactInfo trackImpactInfo = npc.GetSkillStateInfo().GetImpactInfoBySeq(seq);
                if (null != trackImpactInfo) {
                    int targetImpactId = trackImpactInfo.ImpactToTarget;
                    if (targetImpactId <= 0) {
                        targetImpactId = trackImpactInfo.ConfigData.impact;
                    }
                    if (targetImpactId <= 0) {
                        object v;
                        if (args.TryGetValue("impact", out v)) {
                            targetImpactId = (int)v;
                        }
                    }
                    impactId = targetImpactId;
                }
            }
            return impactId;
        }
        public ImpactInfo TrackSendImpact(int targetId, int impactId, int seq, int impactToTarget, Dictionary<string, object> args)
        {
            EntityInfo targetObj = m_Scene.EntityManager.GetEntityInfo(targetId);
            if (null != targetObj && !targetObj.IsDead()) {
                ImpactInfo trackImpactInfo = targetObj.GetSkillStateInfo().GetImpactInfoBySeq(seq);
                if (null != trackImpactInfo && impactId == trackImpactInfo.ImpactId) {
                    ImpactInfo impactInfo = null;
                    if (impactToTarget <= 0 || impactToTarget >= SkillInstance.c_FirstInnerHitSkillId) {
                        impactToTarget = PredefinedSkill.c_HitSkillId;
                        impactInfo = new ImpactInfo(m_Scene.SkillSystem.PredefinedSkill.HitSkillCfg);
                    }
                    var addArgs = new Dictionary<string, object>();
                    if (null == impactInfo) {
                        impactInfo = new ImpactInfo(impactId);
                    }
                    impactInfo.StartTime = TimeUtility.GetLocalMilliseconds();
                    impactInfo.ImpactSenderId = trackImpactInfo.ImpactSenderId;
                    impactInfo.SenderPosition = trackImpactInfo.SenderPosition;
                    impactInfo.SkillId = trackImpactInfo.SkillId;
                    impactInfo.DurationTime = trackImpactInfo.DurationTime > 0 ? trackImpactInfo.DurationTime : impactInfo.ConfigData.impactData.duration;
                    impactInfo.TargetType = trackImpactInfo.TargetType;
                    impactInfo.DamageData.CopyFrom(trackImpactInfo.DamageData);
                    impactInfo.DamageData.Merge(impactInfo.ConfigData.damageData);
                    if (impactInfo.ConfigData.type == (int)SkillOrImpactType.Buff) {
                        ImpactInfo oldImpactInfo = targetObj.GetSkillStateInfo().FindImpactInfoById(impactInfo.ImpactId);
                        if (null != oldImpactInfo) {
                            oldImpactInfo.DurationTime += impactInfo.DurationTime;
                            return oldImpactInfo;
                        }
                    }
                    targetObj.GetSkillStateInfo().AddImpact(impactInfo);
                    m_Scene.SkillSystem.StartSkill(targetId, impactInfo.ConfigData, impactInfo.Seq, args, addArgs);
                    return impactInfo;
                }
            }
            return null;
        }
        public void ImpactDamage(int srcObjId, int targetId, int impactId, int seq)
        {
            EntityInfo targetObj = m_Scene.EntityManager.GetEntityInfo(targetId);
            EntityInfo srcObj = m_Scene.EntityManager.GetEntityInfo(srcObjId);
            if (null != targetObj && !targetObj.IsDead()) {
                ImpactInfo impactInfo = targetObj.GetSkillStateInfo().GetImpactInfoBySeq(seq);
                if (null != impactInfo && impactId == impactInfo.ImpactId) {
                    TableConfig.Skill cfg = impactInfo.ConfigData;
                    int targetType = impactInfo.TargetType;
                    
                    int addsc = impactInfo.DamageData.AddSc;
                    int adduc = impactInfo.DamageData.AddUc;

                    int index = impactInfo.CurDamageCount;
                    ++impactInfo.CurDamageCount;
                    int multiple = impactInfo.DamageData.GetMultiple(index);
                    int damage = impactInfo.DamageData.GetDamage(index);
                    long hitrate = 0;
                    long critrate = 0;
                    long blockrate = 0;
                    long phyDamage = 0;
                    long magDamage = 0;

                    hitrate = AttrCalculator.Calc(targetObj.SceneContext, impactInfo.SenderProperty, targetObj.ActualProperty, "hitrate");
                    critrate = AttrCalculator.Calc(targetObj.SceneContext, impactInfo.SenderProperty, targetObj.ActualProperty, "critrate");
                    blockrate = AttrCalculator.Calc(targetObj.SceneContext, impactInfo.SenderProperty, targetObj.ActualProperty, "blockrate");
                    long rnd = Helper.Random.Next();
                    long critonoff = 0;
                    long blockonoff = 0;
                    if (rnd <= critrate) {
                        critonoff = 1;
                    } else if (rnd > critrate && rnd <= critrate + blockrate) {
                        blockonoff = 1;
                    }
                    impactInfo.DamageData.IsCritical = critonoff > 0;
                    impactInfo.DamageData.IsBlock = blockonoff > 0;
                    phyDamage = AttrCalculator.Calc(targetObj.SceneContext, impactInfo.SenderProperty, targetObj.ActualProperty, "phydamage", multiple, damage, critonoff, blockonoff);
                    magDamage = AttrCalculator.Calc(targetObj.SceneContext, impactInfo.SenderProperty, targetObj.ActualProperty, "magdamage", multiple, damage, critonoff, blockonoff);

                    damage = (int)(phyDamage + magDamage);
                    if (damage < 0)
                        damage = 0;
                    int vampire = impactInfo.DamageData.GetVampire(index);

                    if ((targetType == (int)SkillTargetType.Enemy || targetType == (int)SkillTargetType.RandEnemy) && damage != 0) {
                        if (targetObj.EntityType == (int)EntityTypeEnum.Tower) {
                            if (null != srcObj && srcObj.NormalSkillId != impactInfo.SkillId) {
                                //技能打塔不产生伤害
                                return;
                            }
                        }
                        bool isKiller = false;
                        if (targetObj.Shield >= damage) {
                            targetObj.Shield -= (int)damage;
                        } else if (targetObj.Shield > 0) {
                            int leftDamage = (int)damage - targetObj.Shield;
                            targetObj.Shield = 0;
                            targetObj.Hp -= (int)leftDamage;
                            if (targetObj.Hp <= 0) {
                                isKiller = true;
                            }
                        } else {
                            targetObj.Hp -= (int)damage;
                            if (targetObj.Hp <= 0) {
                                isKiller = true;
                            }
                        }
                        if (isKiller) {
                            targetObj.GetCombatStatisticInfo().AddDeadCount(1);
                            if (null != srcObj) {
                                EntityInfo killer = srcObj;
                                if (killer.SummonerId > 0) {
                                    killer = m_Scene.EntityManager.GetEntityInfo(killer.SummonerId);
                                }
                                if (targetObj.EntityType == (int)EntityTypeEnum.Tower) {
                                    killer.GetCombatStatisticInfo().AddKillTowerCount(1);
                                } else if (targetObj.EntityType == (int)EntityTypeEnum.Hero) {
                                    killer.GetCombatStatisticInfo().AddKillHeroCount(1);
                                    killer.GetCombatStatisticInfo().AddMultiKillCount(1);
                                } else {
                                    killer.GetCombatStatisticInfo().AddKillNpcCount(1);
                                }
                            }
                        }
                        targetObj.SetAttackerInfo(srcObjId, isKiller, true, false, (int)damage, 0);
                    }
                }
            }
        }
        public void KeepTarget(int targetId)
        {
            EntityInfo entity = m_Scene.EntityManager.GetEntityInfo(targetId);
            if (null != entity) {
                entity.CanDead = false;
            }
        }
        public void BornFinish(int objId)
        {
            EntityInfo entity = m_Scene.EntityManager.GetEntityInfo(objId);
            if (null != entity) {
                entity.IsBorning = false;
                entity.SetAIEnable(true);
                entity.RemoveState(CharacterPropertyEnum.x3009_无敌);
            }
        }
        public void DeadFinish(int objId)
        {
            EntityInfo entity = m_Scene.EntityManager.GetEntityInfo(objId);
            if (null != entity) {
                entity.DeadTime = 0;
                entity.NeedDelete = true;
            }
        }

        private bool TryInitImpactInfo(ImpactInfo impactInfo, TableConfig.Skill cfg, int seq, int curObjId, int srcObjId, Dictionary<string, object> args)
        {
            bool ret = false;
            EntityInfo srcNpc = m_Scene.EntityManager.GetEntityInfo(srcObjId);
            impactInfo.StartTime = TimeUtility.GetLocalMilliseconds();
            impactInfo.ImpactSenderId = srcObjId;
            if (cfg.type == (int)SkillOrImpactType.Skill) {
                impactInfo.SenderPosition = null != srcNpc ? srcNpc.GetMovementStateInfo().GetPosition3D() : ScriptRuntime.Vector3.Zero;
                impactInfo.SkillId = cfg.id;
                impactInfo.DurationTime = impactInfo.ConfigData.impactData.duration;
                impactInfo.TargetType = cfg.targetType;
                impactInfo.DamageData.CopyFrom(cfg.damageData);
                impactInfo.DamageData.Merge(impactInfo.ConfigData.damageData);
                impactInfo.ImpactToTarget = cfg.impact;
                ret = true;
            } else {
                ImpactInfo srcImpactInfo = null;
                EntityInfo curObj = m_Scene.EntityManager.GetEntityInfo(curObjId);
                if (null != curObj) {
                    srcImpactInfo = curObj.GetSkillStateInfo().GetImpactInfoBySeq(seq);
                }
                if (null != srcImpactInfo) {
                    //如果当前技能配置有数据则继承当前配置数据，否则继承源impact记录的数据。
                    impactInfo.SenderPosition = srcImpactInfo.SenderPosition;
                    impactInfo.SkillId = srcImpactInfo.SkillId;
                    impactInfo.DurationTime = srcImpactInfo.DurationTime > 0 ? srcImpactInfo.DurationTime : impactInfo.ConfigData.impactData.duration;
                    impactInfo.TargetType = srcImpactInfo.TargetType;
                    impactInfo.DamageData.CopyFrom(srcImpactInfo.DamageData);
                    impactInfo.DamageData.Merge(impactInfo.ConfigData.damageData);
                    impactInfo.ImpactToTarget = cfg.impact != 0 ? cfg.impact : srcImpactInfo.ImpactToTarget;
                    ret = true;
                }
            }
            return ret;
        }

        private void OnDamage(int receiver, int caster, bool isNormalDamage, bool isCritical, int hpDamage, int npDamage)
        {
            if (null != m_Scene && null != m_EntityMgr) {
                EntityInfo npc = m_EntityMgr.GetEntityInfo(receiver);
                int hp = npc.Hp;
                int energy = npc.Energy;
                Msg_RC_ImpactDamage msg = new Msg_RC_ImpactDamage();
                msg.role_id = receiver;
                msg.attacker_id = caster;
                msg.damage_status = 0;
                if (isNormalDamage) {
                    msg.damage_status |= (int)Msg_RC_ImpactDamage.Flag.IS_ORDINARY;
                }
                if (isCritical) {
                    msg.damage_status |= (int)Msg_RC_ImpactDamage.Flag.IS_CRITICAL;
                }
                if (hp <= 0) {
                    msg.damage_status |= (int)Msg_RC_ImpactDamage.Flag.IS_KILLER;
                }
                msg.hp = hp;
                msg.energy = energy;

                m_Scene.NotifyAllUser(RoomMessageDefine.Msg_RC_ImpactDamage, msg);
            }
        }

        public EntityController()
        {
        }


        private Scene m_Scene = null;
        private EntityManager m_EntityMgr = null;
    }
}