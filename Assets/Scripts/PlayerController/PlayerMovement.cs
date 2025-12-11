using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
  [Header("References")]
  public PlayerMovementStats moveStats;
  [SerializeField]private Collider2D coll;


  private Rigidbody2D playerRB;

  //movement variables

  public bool isFacingRight{get;private set;}
  public MovementController controller{get;private set;}
  [HideInInspector]public Vector2 velocity;
  [SerializeField]Transform visualTransform;

  //input 
  private Vector2 moveInput;
  private bool runHeld;
  private bool jumpPressed;
  private bool jumpReleased;
  private bool dashPresed;
  
  //bump head Slide
  private float jumpStartY;
  public bool isheadBumpSliding{get; private set;}
  
  private bool justFinishedSlide;
  private bool slideFromDash;
  private float dashStartY;
  private bool didHeadBumpSlideThisAirnouneState;
  //jump Vars
  private bool isJumping;
  private bool isFastFalling;
  private bool isFalling;
  private float fastFallTime;
  private float fastFallReleaseSpeed;
  private int noOfAirJumpUsed;


  //apex vars
  private float apexPoint;
  private float timePAstApexThreshold;
  private bool isPastApexThreshold;

  //jump buffers vars
  private float jumpBufferTimer;
  private bool jumpReleasedDuringBuffer;

  //coyote Time vars
  private float coyoteTImer;


  //wall slide vars
  [SerializeField]private bool isWallSliding;
  private bool isWallSlideFalling;


  //wall jump vars
  private bool useWallJumpMoveStats;
  private bool isWallJumping;
  private float wallJumpTime;
  private bool iswallJumpFastFalling;
  private float wallJumpFastFallTime;
  private float wallJumpFastFallReleaseSpeed;

  private float wallJumpPostBufferTimer;

  private float wallJumpApexPoint;
  private float timePastWallJumpApexThreshold;
  private bool isPastWallJumpApexThreshold;
  private bool isWallJumpFalling;
  private int lastWallDirection;
  
  //slope
  private bool isPerformingSlopeDash;
  private float slopeDashAngle;


  //dash vars
  public bool isDashing{get;private set;}
  private bool isAirDashing;
  private float dashTimer;
  private float dashOnGroundTimer;
  private int noOfDashUsed;
  private Vector2 dashDirection;
  private bool isDashFastFalling;
  private float dashFastFallTime;
  private float dashFastFallReleaseTime;
  private float dashBufferTimer;

  void Awake()
  {
    isFacingRight=true;

    playerRB=GetComponent<Rigidbody2D>();
    controller=GetComponent<MovementController>();

  }
  private void Update() {

    moveInput=InputManager.movement;
    runHeld=InputManager.runIsHeld;
    if(InputManager.jumpWasPressed)jumpPressed=true;
    if(InputManager.jumpWasReleased)jumpReleased=true;
    if(InputManager.dashWasPressed)dashPresed=true;



  }

  private void FixedUpdate() {
    justFinishedSlide=false;
    CountTimers(Time.fixedDeltaTime);
    JumpChecks();
    LandCheck();
    WallSlideCheck();
    WallJumpCheck();
    DashCheck();
    VelocityReset();

    HandleHorizontalMovement(Time.fixedDeltaTime);
    HandleHeadBumpSlide();
    Jump(Time.fixedDeltaTime);
    Fall(Time.fixedDeltaTime);
    WallSlide(Time.fixedDeltaTime);
    WallJump(Time.fixedDeltaTime);
    Dash(Time.fixedDeltaTime);
    HandleSlide(Time.fixedDeltaTime);
    ClampVelocity();

    controller.Move(velocity*Time.fixedDeltaTime);


    //reset input bool 
    jumpPressed=false;
    jumpReleased=false;
    dashPresed=false;
  }
  private void ClampVelocity(){
        //clamp fall speed
        if (controller.isSliding)
        {
            velocity.y=Mathf.Clamp(velocity.y,-moveStats.wallSlideSpeed,50f);
        }
        else if (isDashing)
        {
          velocity.y = Mathf.Clamp(velocity.y, -50f, 50f);

        }
        else
        {
          velocity.y = Mathf.Clamp(velocity.y, -moveStats.maxFallSpeed, 50f);
            
        }
  }
  #region Movement

  private void HandleHorizontalMovement(float timeStep){
        if (isheadBumpSliding)
        {
            return;
        }
    if(!isDashing){
      
        float acceleration=controller.IsGrounded()?moveStats.groundAcceleration:moveStats.airAcceleration;
        float decelaration=controller.IsGrounded()?moveStats.groundDeceleration:moveStats.airDeceleration;
        if (useWallJumpMoveStats)
            {
                acceleration=moveStats.wallJumpMoveAccelaration;
                decelaration=moveStats.wallJumpMoveDccelaration;
            }
        if(Mathf.Abs(moveInput.x)>=moveStats.moveThreshold){
            TurnCheck(moveInput);

            float moveDir=Mathf.Sign(moveInput.x);
            float targetVelocity=runHeld?moveDir*moveStats.maxRunSpeed :moveDir*moveStats.maxWalkSpeed;
            float t=Mathf.Clamp01(acceleration*timeStep);
            velocity.x=Mathf.Lerp(velocity.x,targetVelocity,t);

                if (Mathf.Abs(velocity.x - targetVelocity) <= 0.01f)
                {
                    velocity.x=targetVelocity;
                }
        }
        else
        {   
            float t=Mathf.Clamp01(decelaration*timeStep);
            velocity.x=Mathf.Lerp(velocity.x,0,t);

                if (Mathf.Abs(velocity.x) <= 0.01f)
                {
                    velocity.x=0f;
                }
        }

  }
  }
#endregion

  #region Jump
  private void ResetJumpValues(){
    isJumping=false;
    isFalling=false;
    isFastFalling=false;
    fastFallTime=0f;
    isPastApexThreshold=false;

  }
  private void JumpChecks(){
    //when jump btn is pressed
    if(jumpPressed){
      if(isWallSlideFalling && wallJumpPostBufferTimer >= 0f)
      {
        return;
      }


      else if(isWallSliding ||(controller.IsTouchingWall(isFacingRight) && !controller.IsGrounded()))
      {
        return;
      }
      jumpBufferTimer=moveStats.jumpBufferTime;
      jumpReleasedDuringBuffer=false;
    }
    //when jump btn is released
    if(jumpReleased){
      if(jumpBufferTimer>0f){
        jumpReleasedDuringBuffer=true;

      }
      if(isJumping&&velocity.y>0f){
        if(isPastApexThreshold){
          isPastApexThreshold=false;
          isFastFalling=true;
          fastFallTime=moveStats.timeForUpwardCancel;
          velocity.y=0f;
        }else{
          isFastFalling=true;
          fastFallReleaseSpeed=velocity.y;
        }
      }
    }

    //initiate jump with jump buffering and coyote time
    if(jumpBufferTimer>0f && !isJumping && (controller.IsGrounded()||coyoteTImer>0f) && (moveStats.canJumponMaxSlopes ||controller.SlopeAngle<=moveStats.maxSlopeAngle )){
        InitiateJump(0);

        if(jumpReleasedDuringBuffer){
           isFastFalling=true;
           fastFallReleaseSpeed=velocity.y;
        }
    }

    //double jump
    else if(jumpBufferTimer>0f  && (isJumping || isWallJumping || isWallSlideFalling || isAirDashing || isDashFastFalling || controller.isSliding) && 
            !controller.IsTouchingWall(isFacingRight) &&noOfAirJumpUsed<moveStats.noOfJumpAirAllowed){
      isFastFalling=false;
      InitiateJump(1);

            if (isDashFastFalling)
            {
                isDashFastFalling=false;

            }
    }
    //air jump after coyote time lapesed
    else if(jumpBufferTimer>0f && isFalling && !isWallSlideFalling && noOfAirJumpUsed<moveStats.noOfJumpAirAllowed){
      InitiateJump(1);
      isFastFalling=false;

    }
    
  }
 

  private void InitiateJump(int noOfAirJumpUsed){
    if(!isJumping){
      isJumping=true;
    }
    
    ResetWallJumpVAlues();
    jumpBufferTimer=0f;
    this.noOfAirJumpUsed+=noOfAirJumpUsed;
    velocity.y=moveStats.initialJumpVelocity;
    didHeadBumpSlideThisAirnouneState=false;
    jumpStartY=playerRB.position.y;

  }
  private void Jump(float timestep)
    {
        //apply gravity while jumping
        if (isJumping)
        {
            // check for head bump
            if (controller.BumpedHead() &&!isheadBumpSliding)
            {
                if(controller.headBumpedSldieDirection!=0 &&!controller.isHitingCeilingCenter && !controller.isHittingBothCorners)
                {
                    slideFromDash=false;
                    
                }
                else if(moveStats.jumpFollowSlopesWhenHeadTouching && controller.ceilingAngle>0f)
                {
                    Vector2 ceilingNormal=controller.ceilingNormal;
                    velocity=velocity-(Vector2.Dot(velocity,ceilingNormal)*ceilingNormal);
                }
                else
                {
                    velocity.y = 0f;
                    isFastFalling=true;
                }
            }

            if (isheadBumpSliding)
            {
                velocity.y=0;
                return;
            }

            if (!justFinishedSlide)
            {
                //gravity on ascending
            if (velocity.y >= 0f)
            {
                //apex control
                apexPoint = Mathf.InverseLerp(moveStats.initialJumpVelocity, 0f, velocity.y);

                if (apexPoint > moveStats.apexThreshold)
                {
                    if (!isPastApexThreshold)
                    {
                        isPastApexThreshold = true;
                        timePAstApexThreshold = 0f;

                    }
                    if(isPastApexThreshold)
                    {
                        timePAstApexThreshold += timestep;
                        if (timePAstApexThreshold < moveStats.apexHangTime)
                        {
                            velocity.y = 0f;

                        }
                        else
                        {
                            velocity.y = -0.01f;
                        }
                    }
                }
                //gravity on ascending without past apex threshold
                else if(!isFastFalling)
                {
                    velocity.y += moveStats.gravity * timestep;
                    if (isPastApexThreshold)
                    {
                        isPastApexThreshold = false;

                    }
                }

            }
            //gravity on descending
            else if (!isFastFalling)
            {
                velocity.y += moveStats.gravity * moveStats.gravityOnReleaseMultiplier * timestep;
            }
            else if (velocity.y < 0f)
            {
                if (!isFalling)
                {
                    isFalling = true;
                }
            }


        }
  }

            




        //jump cut
        if (isFastFalling)
        {
            if (fastFallTime >= moveStats.timeForUpwardCancel)
            {
                velocity.y += moveStats.gravity * moveStats.gravityOnReleaseMultiplier * timestep;

            }else if (fastFallTime < moveStats.timeForUpwardCancel)
            {
                velocity.y = Mathf.Lerp(fastFallReleaseSpeed, 0f, (fastFallTime / moveStats.timeForUpwardCancel));

            }
            fastFallTime+= timestep;
        }
        
    }

  #endregion

  #region Timers
  private void CountTimers(float timeStep){
    jumpBufferTimer-=timeStep;
    HandleCoyoteTimer(timeStep);
    wallJumpPostBufferTimer -=timeStep;

    if (controller.IsGrounded())
    {
        dashOnGroundTimer -=timeStep;
    }

    //dash buffer timer
    dashBufferTimer -=timeStep;
  }

  private void HandleCoyoteTimer(float timeStep)
    {
        if(controller.IsGrounded() && !controller.isSliding && !IsSlideableSlope(controller.SlopeAngle))
        {
            coyoteTImer=moveStats.jumpCoyoteTime;
        }
        else
        {
            coyoteTImer-=timeStep;
        }
    }

    private void HandleDashOnGroundTimer(float timestep)
    {
        if(controller.IsGrounded() && !controller.isSliding && !IsSlideableSlope(controller.SlopeAngle))
        {
            dashOnGroundTimer-=timestep;
        }
    }

  #endregion

#region TurnChecking
   private void TurnCheck(Vector2 moveInput)
    {
        if (isFacingRight && moveInput.x < 0)
        {
            Turn(false);
           // camerasmooth.callTurn();
        }else if(!isFacingRight && moveInput.x > 0)
        {
            Turn(true);
           // camerasmooth.callTurn();
        }
    }

    private void Turn(bool TurnRight)
    {
        if (TurnRight)
        {
            isFacingRight = true;
            visualTransform.Rotate(0f, 180f, 0f);
        }
        else
        {
            isFacingRight = false;
            visualTransform.Rotate(0f, -180f, 0f);
        }
       
    }


    private void HandleHeadBumpSlide()
    {
        
        if(!isheadBumpSliding &&!didHeadBumpSlideThisAirnouneState &&(isJumping||isDashing || isWallJumping) && controller.BumpedHead() && !controller.isHittingBothCorners && !controller.isHitingCeilingCenter)
        {
            if(isWallSliding|| controller.isSliding)
            {
                return;
            }

            if (controller.ceilingAngle <= moveStats.maxSlopeAngleForHeadBump)
            {
                
                isheadBumpSliding=true;
                didHeadBumpSlideThisAirnouneState=true;
            }
     
        }

        if (isheadBumpSliding)
        {
            velocity.y=0f;

            if(controller.headBumpedSldieDirection==0 || !controller.BumpedHead() || controller.isHitingCeilingCenter || controller.isHittingBothCorners)
            {
                isheadBumpSliding=false;
                velocity.x=0;


                if(!slideFromDash)
                {
                    float compensationFactor=(1-moveStats.jumpHeightCompensationFactor)+1;
                    float jumpPeakY=jumpStartY +(moveStats.jumpHeight*compensationFactor);
                    float remainingHeight=jumpPeakY -playerRB.position.y;
                    
                    if(remainingHeight>0)
                    {
                        float requiredVelocity=Mathf.Sqrt(2*Mathf.Abs(moveStats.gravity)* remainingHeight);
                        velocity.y=requiredVelocity;
                      
                    }
                }
                else if (slideFromDash)
                {
                    float targetApexY=dashStartY +moveStats.dashTargetApexHeight;
                    float remainingHeight=targetApexY -playerRB.position.y;


                    
                    if(remainingHeight>0)
                    {
                        float requiredVelocity =Mathf.Sqrt(2*Mathf.Abs(moveStats.gravity)*remainingHeight);
                        velocity.y=requiredVelocity;

                    }
                }
            justFinishedSlide=true;
            slideFromDash=false;
          
            }
            else
            {
                  velocity.x=controller.headBumpedSldieDirection*moveStats.headBumpSlideSpeed;
                
            }
        }
          
          
           
        

    }
#endregion


 #region  LandCheck/Fall
  private void LandCheck(){

        if (controller.IsGrounded())
        {


            bool isGroundAWall=controller.SlopeAngle>=moveStats.minAngleForWallSlide && controller.SlopeAngle <=moveStats.maxAngleForWallSlide;

            if (isGroundAWall)
            {
                return;
            }
          if((isJumping||isFalling || isWallJumpFalling || isWallJumping || isWallSlideFalling || isWallSlideFalling || isDashFastFalling ||isheadBumpSliding)  && velocity.y<=0f){
            isheadBumpSliding=false;
            didHeadBumpSlideThisAirnouneState=false;
            ResetJumpValues();
            StopWallSlide();
            ResetWallJumpVAlues();
            ResetDashes();
            ResestDashValues();

            
          }
          if(moveStats.resetAirJumpOnMaxSlopesLand|| (moveStats.resetAirJumpOnMaxSlopesLand && controller.SlopeAngle <= moveStats.maxSlopeAngle))
                {
                        noOfAirJumpUsed=0;
                    
                }

            
           
        }
  }
  private void Fall(float timestep){
    //normal gravity while falling

        if(!controller.IsGrounded()&&!isJumping &&!isWallSliding && !isDashing && !isDashFastFalling && !isWallJumping)
        {
            if (!isFalling)
                isFalling = true;
            velocity.y += moveStats.gravity * timestep;
        }
  }


    private void VelocityReset()
    {
        if(controller.isSliding)return;

        if (controller.IsGrounded())
        {
            if(!IsSlideableSlope(controller.SlopeAngle) && !controller.isOnSlideableSlope)
            {
                if (velocity.y <= 0f)
                {
                    velocity.y=-2f;
                }
            }
        }
    }
  #endregion


  #region Wall Slide
  private void WallSlideCheck(){

    bool IsTouchingSideWall =controller.IsTouchingWall(isFacingRight);
    bool isSideWallAngle= controller.wallAngle >=moveStats.minAngleForWallSlide && controller.wallAngle <=moveStats.maxAngleForWallSlide;

    if(!isDashing&& IsTouchingSideWall&& isSideWallAngle && !controller.IsGrounded() ){
      if(velocity.y<0f && !isWallSliding){
        ResetJumpValues();
        ResetWallJumpVAlues();
        ResestDashValues();

        if(moveStats.resetDashOnWallSlide){
          ResetDashes();
        }
        isWallSlideFalling=false;
        isWallSliding=true;
        if(moveStats.resetJumpOnWallSlide){
          noOfAirJumpUsed=0;
        }
      }
    }else if(isWallSliding && !IsTouchingSideWall){
      isWallSlideFalling=true;
      StopWallSlide();
    }
    else{
      StopWallSlide();
    }
  }
  private void StopWallSlide(){
    if(isWallSliding){
      //varies with different game
      //if player fall consume 1 jump only give 1 air jump
      isWallSliding=false;
    }
  }
  private void WallSlide(float timeStep){
    if(isWallSliding){
      velocity.y=Mathf.Lerp(velocity.y,-moveStats.wallSlideSpeed,moveStats.wallSlideDecelaration*timeStep);
    }
  }
  #endregion

  #region Wall Jump


  private void WallJumpCheck()
  {
        if (ShouldApplyWallJumpBuffer())
        {
            wallJumpPostBufferTimer =moveStats.wallJumpBufferTime;
        }

        //wall jump fast falling
        if(jumpReleased && !isWallSliding && !controller.IsTouchingWall(isFacingRight) && isWallJumping)
        {
            if(velocity.y> 0f)
            {
                if (isPastWallJumpApexThreshold)
                {
                    isPastWallJumpApexThreshold=false;
                    iswallJumpFastFalling=true;
                    wallJumpFastFallTime =moveStats.timeForUpwardCancel;

                    velocity.y =0f;
                }
                else
                {
                    iswallJumpFastFalling =true;
                    wallJumpFastFallReleaseSpeed = velocity.y;

                }
            }
        }

        //actual jump with post wall buffer
        if(jumpPressed && wallJumpPostBufferTimer > 0f)
        {
            InitiateWallJump();
            Debug.Log("Initiate Wall Jump");
        }

  }

    private void InitiateWallJump()
    {
        if (!isWallJumping)
        {
            isWallJumping=true;
            useWallJumpMoveStats =true;

        }
        StopWallSlide();
        ResetJumpValues();
        wallJumpTime =0f;

        velocity.y = moveStats.initialWallJumpVelocity;

        velocity.x =Mathf.Abs(moveStats.wallJumpDirection.x)* -lastWallDirection;
        didHeadBumpSlideThisAirnouneState=false;
        jumpStartY=playerRB.position.y;
    }
  private void WallJump(float timeStep)
  {

        //apply wall jump gravity 
        if (isWallJumping)
        {
            wallJumpTime+=timeStep;

            if(wallJumpTime >= moveStats.timeTillJumpApex)
            {
                useWallJumpMoveStats=false;
            }


            //hit Head
            if (controller.BumpedHead() && !isheadBumpSliding)
            {
                if (controller.headBumpedSldieDirection != 0 && !controller.isHitingCeilingCenter && !controller.isHittingBothCorners)
                {
                    slideFromDash=false;
                    
                }
                else if(moveStats.jumpFollowSlopesWhenHeadTouching && controller.ceilingAngle>0f)
                {
                    Vector2 ceilingNormal=controller.ceilingNormal;
                    velocity=velocity-(Vector2.Dot(velocity,ceilingNormal)*ceilingNormal);
                }
                else
                {
                    velocity.y = 0f;
                    iswallJumpFastFalling = true;
                    useWallJumpMoveStats =false;
                }
            }
            if (isheadBumpSliding)
            {
                velocity.y = 0;
                return;
            }
            if (!justFinishedSlide)
            {
                //gravity in acsending
            if(velocity.y >= 0f)
            {
                //apex control
                wallJumpApexPoint =Mathf.InverseLerp(moveStats.wallJumpDirection.y ,0f ,velocity.y );

                if(wallJumpApexPoint > moveStats.apexThreshold)
                {
                    if (!isPastWallJumpApexThreshold)
                    {
                        isPastWallJumpApexThreshold =true;
                        timePastWallJumpApexThreshold=0f;

                    }

                    if (isPastWallJumpApexThreshold)
                    {
                        timePastWallJumpApexThreshold +=timeStep;
                        if(timePastWallJumpApexThreshold < moveStats.apexHangTime)
                        {
                            velocity.y =0f;

                        }
                        else
                        {
                            velocity.y  -=0.01f;
                        }
                    }
                }

                //gravity in ascending but not past apex threshold
                else if (!iswallJumpFastFalling)
                {
                    velocity.y  +=moveStats.wallJumpGravity * timeStep;

                    if (isPastWallJumpApexThreshold)
                    {
                        isPastWallJumpApexThreshold=false;
                    }
                }
            }



            //gravity ion descending
            else if (!iswallJumpFastFalling)
            {
                velocity.y  +=moveStats.wallJumpGravity *timeStep;
            }

            else if(velocity.y  < 0f)
            {
                if (!isWallJumpFalling)
                {
                    isWallJumpFalling =true;
                }
            }
        }
            }

            


        //handle wall jump cut time 
        if (iswallJumpFastFalling)
        {
            if(wallJumpFastFallTime >= moveStats.timeForUpwardCancel)
            {
                velocity.y  +=moveStats.wallJumpGravity *moveStats.wallJumpGravityOnReleaseMultiplier *timeStep;
            }else if(wallJumpFastFallTime < moveStats.timeForUpwardCancel)
            {
                velocity.y  =Mathf.Lerp(wallJumpFastFallReleaseSpeed ,0f,wallJumpFastFallTime/moveStats.timeForUpwardCancel);
            }

            wallJumpFastFallTime+=timeStep;
        }

  }

  private bool ShouldApplyWallJumpBuffer()
    {

       bool isWallAngleValid=controller.wallAngle>=moveStats.minAngleForWallSlide && controller.wallAngle<=moveStats.maxAngleForWallSlide;

      if(controller.IsTouchingWall(isFacingRight) && isWallAngleValid|| isWallSliding)
      {
          lastWallDirection=controller.GetWallDirection();
          return true;
      }
      else
      {
        return false;      
      }
         
    }
  private void ResetWallJumpVAlues(){
    isWallSlideFalling=false;
    useWallJumpMoveStats=false;
    isWallJumping=false;
    iswallJumpFastFalling=false;
    isPastWallJumpApexThreshold=false;
    isWallJumpFalling=false;

    wallJumpFastFallTime=0f;
    wallJumpTime=0f;
  }
  #endregion

  #region Dash
  private void ResestDashValues(){
    isDashFastFalling=false;
    dashOnGroundTimer=-1f;

    dashFastFallReleaseTime=0;
    dashFastFallTime=0f;
    dashDirection=Vector2.zero;
    isPerformingSlopeDash=false;
  }
  private void ResetDashes(){
    noOfDashUsed=0;
  }


  private void DashCheck()
  {
        if (dashPresed)
        {
            dashBufferTimer=moveStats.dashBufferTime;
        }
        if (dashBufferTimer>0)
        {
            //ground dash
            if(controller.IsGrounded() && dashOnGroundTimer<0 && !isDashing)
            {
                InitiateDash();
                dashBufferTimer=0f;
            }
            //air dash
            else if (!controller.IsGrounded() && !isDashing && noOfDashUsed < moveStats.noOfDashes)
            {
                isAirDashing=true;
                InitiateDash();
                dashBufferTimer=0f;


                
            }
        }
  }

  private void InitiateDash()
    {
      dashStartY=playerRB.position.y;

        dashDirection=moveInput;
        TurnCheck(dashDirection);

        Vector2 closestDirection=Vector2.zero;  
        float minDistance=Vector2.Distance(dashDirection,moveStats.dashDirections[0]);

        for(int i = 0; i < moveStats.dashDirections.Length; i++)
        {
            //skip if we hit it bang on 
            if(dashDirection == moveStats.dashDirections[i])
            {
                closestDirection=dashDirection;
                break;
            }

            float distance=Vector2.Distance(dashDirection,moveStats.dashDirections[i]);

          //check if this is a diagonal direction and apply bias
          bool isDiagonal=(Mathf.Abs(moveStats.dashDirections[i].x) == 1 && Mathf.Abs(moveStats.dashDirections[i].y)==1);
            if (isDiagonal)
            {
                distance -=moveStats.dashDiagonallyBias;
            }
            else if(distance < minDistance)
            {
                minDistance=distance;
                closestDirection=moveStats.dashDirections[i];
            }
        }


        //handle dir with no input 
        if(closestDirection == Vector2.zero)
        {
            if (isFacingRight)
            {
                closestDirection=Vector2.right;
            }
            else
            {
                closestDirection=Vector2.left;
            }
        }

        if(controller.IsGrounded() && closestDirection.y<0f &&closestDirection.x!=0)
        {
            closestDirection=new Vector2(Mathf.Sign(closestDirection.x),0);
        }

        dashDirection=closestDirection;
        noOfDashUsed++;
        isDashing=true;
        dashTimer =0f;
        dashOnGroundTimer =moveStats.timeBtwDashOnGround;

        ResetJumpValues();
        ResetWallJumpVAlues();
        StopWallSlide();

        if (dashDirection.y > 0)
        {
            didHeadBumpSlideThisAirnouneState=false;
        }



        isPerformingSlopeDash=controller.IsGrounded() && controller.SlopeAngle>0 && dashDirection.y==0 && !isJumping && Mathf.Sign(dashDirection.x)!=Mathf.Sign(controller.slopeNormal.x);
        if (isPerformingSlopeDash)
        {
            slopeDashAngle=controller.SlopeAngle;
        }

    }
  private void Dash(float timeStep)
  {
        if(justFinishedSlide)return;
        //stop the timer after the dash

        if (isDashing)
        {

          if(controller.BumpedHead() && !isheadBumpSliding)
            {
                if(controller.headBumpedSldieDirection!=0 && !controller.isHitingCeilingCenter && !controller.isHittingBothCorners)
                {
                    
                    slideFromDash= true;
             
                    dashTimer=0f;
                    


                }
                else if(moveStats.dashFollowSlopesWhenheadTouching && controller.ceilingAngle>0f)
                {
                    Vector2 ceilingNormal=controller.ceilingNormal;
                    velocity=velocity-(Vector2.Dot(velocity,ceilingNormal)*ceilingNormal);
                }
                else
                {
                    velocity.y=0;
                    isDashing=false;
                    isAirDashing=false;
                    dashTimer=0;

                }
            }

            if (isheadBumpSliding)
            {
                velocity.y=0f;
                return;
            }
          //stop dash after timer
          dashTimer +=timeStep;
          if(dashTimer >= moveStats.dashTime)
            {
                if (controller.IsGrounded())
                {
                    ResetDashes();

                }

                isAirDashing=false;
                isDashing=false;

                if(!isJumping && !isWallJumping)
                {
                    dashFastFallTime = 0f;
                    dashFastFallReleaseTime=velocity.y;


                    if (!controller.IsGrounded())
                    {
                        isDashFastFalling=true;

                    }
                    else
                    {
                        velocity.y=0f;
                    }
                }

                return;
            }

            if(moveStats.dashDirectionMatchesSlopesDirection &&isPerformingSlopeDash)
            {
                velocity.x=Mathf.Cos(slopeDashAngle * Mathf.Deg2Rad)*moveStats.dashSpeed*dashDirection.x;
                velocity.y=Mathf.Sin(slopeDashAngle * Mathf.Deg2Rad)*moveStats.dashSpeed;
            }
            else
            {
                velocity.x=moveStats.dashSpeed * dashDirection.x;

                if(dashDirection.y !=0f || isAirDashing)
                {
                    velocity.y =moveStats.dashSpeed *dashDirection.y;
                }else if(!isJumping && dashDirection.y == 0f)
                {
                    velocity.y=-0.001f;
                }
                
            }

            #region dash Debug Visualization
            if (moveStats.debugShowDashAngle)
            {
                Vector2 drawOrigin=coll.bounds.center;
                Vector2 drawDirection=velocity.normalized;
                float drawLength=moveStats.ExtraRayDebug *4f;

                Debug.DrawRay(drawOrigin,drawDirection *drawLength ,Color.cyan);
            }
            #endregion


        }

        //handle dash cut
        else if (isDashFastFalling)
        {
            if(velocity.y > 0f)
            {
                if(dashFastFallTime < moveStats.dashTimeForUpwardCancel)
                {
                    velocity.y= Mathf.Lerp(dashFastFallReleaseTime,0f,dashFastFallTime/moveStats.dashTimeForUpwardCancel);
                }
                else if(dashFastFallTime >=moveStats.dashTimeForUpwardCancel)
                {
                    velocity.y +=moveStats.gravity * moveStats.dashGravityOnReleaseMultiplier *timeStep;
                }

                dashFastFallTime +=timeStep;
            }
            else
            {
                  velocity.y +=moveStats.gravity * moveStats.dashGravityOnReleaseMultiplier *timeStep;
            }
        }

  }

    #endregion

    #region  Slide
    private void HandleSlide(float timeStep)
    {
        if (controller.isSliding)
        {
            if(isJumping)return;

            if(isWallJumping)return;

            velocity.y+=moveStats.gravity*timeStep;
        }
    }

    #endregion

    void OnDrawGizmos()
    {
        if (moveStats.showWalkJumpArc)
        {
            DrawJumpArc(moveStats.maxWalkSpeed,Color.white);
        }

        if (moveStats.showRunJumpArc)
        {
            DrawJumpArc(moveStats.maxRunSpeed,Color.red);
        }
    }



    #region JumpVisualization
    private void DrawJumpArc(float moveSpeed, Color gizmoColor)
{
    Vector2 startPosition = new Vector2(coll.bounds.center.x, coll.bounds.min.y);
    Vector2 previousPosition = startPosition;
    float speed = 0f;
    if (moveStats.drawRight)
    {
        speed = moveSpeed;
    }
    else speed = -moveSpeed;
    Vector2 velocity = new Vector2(speed, moveStats.initialJumpVelocity);

    Gizmos.color = gizmoColor;

    float timeStep = 2 * moveStats.timeTillJumpApex / moveStats.arcResolutuion; // time step for the simulation
    // float totalTime = (2 * MoveStats.TimeTillJumpApex) + MoveStats.ApexHangTime; // total time of the arc including hang time

    for (int i = 0; i < moveStats.visualizationStep; i++)
    {
        float simulationTime = i * timeStep;
        Vector2 displacement;
        Vector2 drawPoint;

        if (simulationTime < moveStats.timeTillJumpApex) // Ascending
        {
            displacement = velocity * simulationTime + 0.5f * new Vector2(0, moveStats.gravity) * simulationTime * simulationTime;
        }
        else if (simulationTime < moveStats.timeTillJumpApex + moveStats.apexHangTime) // Apex hang time
        {
            float apexTime = simulationTime - moveStats.timeTillJumpApex;
            displacement = velocity * moveStats.timeTillJumpApex + 0.5f * new Vector2(0, moveStats.gravity) * moveStats.timeTillJumpApex * moveStats.timeTillJumpApex;
            displacement += new Vector2(speed, 0) * apexTime; // No vertical movement during hang time
        }
        else // Descending
        {
            float descendTime = simulationTime - (moveStats.timeTillJumpApex + moveStats.apexHangTime);
            displacement = velocity * moveStats.timeTillJumpApex + 0.5f * new Vector2(0, moveStats.gravity) * moveStats.timeTillJumpApex * moveStats.timeTillJumpApex;
            displacement += new Vector2(speed, 0) * moveStats.apexHangTime; // Horizontal movement during hang time
            displacement += new Vector2(speed, 0) * descendTime + 0.5f * new Vector2(0, moveStats.gravity) * descendTime * descendTime;
        }

        drawPoint = startPosition + displacement;

        if (moveStats.stopOnCollision)
        {
            RaycastHit2D hit = Physics2D.Raycast(previousPosition, drawPoint - previousPosition, Vector2.Distance(previousPosition, drawPoint), moveStats.GroundLayer);
            if (hit.collider != null)
            {
                // If a hit is detected, stop drawing the arc at the hit point
                Gizmos.DrawLine(previousPosition, hit.point);
                break;
            }

        }
       
        

        Gizmos.DrawLine(previousPosition, drawPoint);
        previousPosition = drawPoint;
    }
  }

  #endregion


  #region HelperMethod
  private bool IsSlideableSlope(float slopeAngle)
    {
        if(slopeAngle >=moveStats.maxSlopeAngle && slopeAngle < moveStats.minAngleForWallSlide)
        {
            return true;
        }
        return false;
    }
  #endregion
  
  
}
    
    




    
    




