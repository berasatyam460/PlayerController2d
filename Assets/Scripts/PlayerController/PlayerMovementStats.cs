using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[CreateAssetMenu(menuName ="PlayerMovementStats")]
public class PlayerMovementStats : ScriptableObject
{
   public static PlayerMovementStats stats;
   #region Walk
      

   [Header("Walk")]
   [Range(0f,1f)]public float moveThreshold=0.25f;
   [Range(1f,100f)]public float maxWalkSpeed=12f;
   [Range(0.25f,50f)]public float groundAcceleration=5f;
   [Range(0.25f,50f)]public float groundDeceleration=20f;
   [Range(0.25f,50f)]public float airAcceleration=5f;
   [Range(0.25f,50f)]public float airDeceleration=5f;
   #endregion
   #region Wall Jump Move
   [Header("Wall Jump Move")]
   [Range(0.25f,50f)]public float wallJumpMoveAccelaration=5f;
   [Range(0.25f,50f)]public float wallJumpMoveDccelaration=5f;

   #endregion

   #region Run
   [Header("Run")]
   [Range(1f,100f)]public float maxRunSpeed=20f;
   #endregion

   #region Collision Detection
   [Header("Collision Detection")]
   public LayerMask GroundLayer;
   
   #endregion

   #region Jump
   [Header("Jump")]
   public float jumpHeight=6.5f;
   [Range(1f,1.1f)]public float jumpHeightCompensationFactor=1.054f;
   public float timeTillJumpApex=0.35f;
   [Range(0.01f,5f)]public float gravityOnReleaseMultiplier=2f;
   public float maxFallSpeed=26f;
   [Range(0,5)]public int noOfJumpAirAllowed=1;


   public bool isDoubleJumpAllowed=false;
   #endregion
   #region JUmp OPtion
   
   [Header("Reset Jump Options")]
   public bool resetJumpOnWallSlide=true;
   public bool resetAirJumpOnMaxSlopesLand=false;


   [Header("Jump Cuts")]
   [Range(0.02f,0.3f)]public float timeForUpwardCancel=0.027f;

   [Header("Jump Apex")]
   [Range(0.5f,1f)]public float apexThreshold=0.97f;
   [Range(0.01f,1f)]public float apexHangTime=0.075f;

   [Header("Jump Buffer")]
   [Range(0f,1f)]public float jumpBufferTime=0.125f;

   [Header("Jump Coyote Time")]
   [Range(0f,1f)]public float jumpCoyoteTime=0.1f;
   
   #endregion

   #region  Wall Slide
   
   [Header("Wall Slide")]
   [Min(0.01f)]public float wallSlideSpeed=5f;
   [Range(0.25f,50f)]public float wallSlideDecelaration=50f;
   [Range(70f,90f)]public float minAngleForWallSlide=85f;
   [Range(90f,135f)]public float maxAngleForWallSlide=95f;

   #endregion
   #region  Wall Jump
   [Header("Wall Jump Stats")]
   public Vector2 wallJumpDirection=new Vector2(-20f,3.5f);
   [Range(0f,1f)]public float wallJumpBufferTime=0.125f;
   [Range(0.01f,5f)]public float wallJumpGravityOnReleaseMultiplier=1f;
   #endregion

   #region head Bump Slide
   public bool useHeadBumpSlide=true;
   [Range(1f,50f)]public float headBumpSlideSpeed=13f;
   [Range(0.01f,1f)]public float HeadBumpBoxWidth=0.3f;
   [Range(0.01f,0.5f)]public float HeadBumpBoxHeight=0.1f;
   [Range(0f,45f)]public float maxSlopeAngleForHeadBump=5f;

   #endregion

   #region  Slopes
   [Header("Slopes")]
   public bool dashDirectionMatchesSlopesDirection=true;
   public bool canJumponMaxSlopes=false;
   public bool jumpFollowSlopesWhenHeadTouching=true;
   public bool dashFollowSlopesWhenheadTouching=true;
   [Range(0f,90f)]public float maxSlopeAngle=70f;
   [Range(1f,100f)]public float maxSlideSpeed=30f;
   #endregion
   #region Dash
   [Header("Dash")]
   [Range(0f,1f)]public float dashTime=0.11f;
   [Range(1f,100f)]public float dashSpeed=40f;
   [Range(0f,1f)]public float timeBtwDashOnGround=0.225f;
   public bool resetDashOnWallSlide=true;
   [Range(0,5)]public int noOfDashes=2;
   [Range (0f,5f)]public float dashDiagonallyBias=0.4f;
   [Range(0f,1f)]public float dashBufferTime=0.125f;
   
   
   [Header("Dash Cancel Time")]
   [Range(0.01f,5f)]public float dashGravityOnReleaseMultiplier=1f;
   [Range(0.02f,0.3f)]public float dashTimeForUpwardCancel=0.07f;
   #endregion

   [Header("Debug")]
  public bool debugShowIsGrounded;
  public bool debugShowHeadRays;
  public bool debugShowWallHit;
  public bool debugShowHeadBumpBox;
   [Range(0f,1f)]public float ExtraRayDebug=0.25f;
   public bool debugShowDecedingSlopeRay;
   public bool debugShowSlopeNormal;
   public bool debugShowDashAngle;
   

   [Header("Jump Visualization Tool")]
   public bool showWalkJumpArc=false;
   public bool showRunJumpArc=false;
   public bool stopOnCollision=true;
   public bool drawRight=true;
   [Range(5,100)]public int arcResolutuion=20;
   [Range(0,500)]public int visualizationStep=90;


  public readonly Vector2[] dashDirections=new Vector2[]{
       new Vector2(0,0),//nothing
       new Vector2(1,0),//right
       new Vector2(1,1).normalized,//top right
       new Vector2(0,1),//top
       new Vector2(-1,1).normalized,//top left
       new Vector2(-1,0),//left
       new Vector2(-1,-1).normalized,//bottomleft
       new Vector2(0,-1),//bottom
       new Vector2(1,-1).normalized// bottom right
  };
   public float gravity{get;private set;}
   public float initialJumpVelocity{get;private set;}
   public float adjustedJumpHeight{get;private set;}
   public float wallJumpGravity{get;private set;}
   public float initialWallJumpVelocity{get;private set;}
   public float adjustedWallJumpHeight{get;private set;}
   
   
   //dash
   public float dashTargetApexHeight{get;private set;}
   private void OnValidate() {
      CalculateVAlues();

      stats=this;
      
      }

   private void OnEnable() {
      CalculateVAlues();

      stats=this;
   }

   private void CalculateVAlues(){
      //normal gravity
      adjustedJumpHeight=jumpHeight*jumpHeightCompensationFactor;
      gravity=-(2f*adjustedJumpHeight)/Mathf.Pow(timeTillJumpApex,2f);
      initialJumpVelocity=Mathf.Abs(gravity)*timeTillJumpApex;

      //wall jump
      adjustedWallJumpHeight=wallJumpDirection.y*jumpHeightCompensationFactor;
      wallJumpGravity=-(2f*adjustedWallJumpHeight)/Mathf.Pow(timeTillJumpApex,2f);
      initialWallJumpVelocity=Mathf.Abs(wallJumpGravity)*timeTillJumpApex;


      //dash
      float step=Time.fixedDeltaTime;
      float dashTimeRounded=Mathf.Ceil(dashTime/step)*step;
      float dashCancelTimeRounded=Mathf.Ceil(dashTimeForUpwardCancel/step)*step;

      float dashConstantPhaseHeight=dashSpeed *dashTimeRounded;

      float dashCancelPhaseHeight=0.5f *dashSpeed *dashCancelTimeRounded;
      dashTargetApexHeight=dashConstantPhaseHeight+dashCancelPhaseHeight;
   }
}