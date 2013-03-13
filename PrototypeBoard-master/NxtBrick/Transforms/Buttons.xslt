<?xml version="1.0" encoding="UTF-8" ?>
<!-- 
    This file is part of Microsoft Robotics Developer Studio Code Samples.
    Copyright (C) Microsoft Corporation.  All rights reserved.
    $File: Buttons.xslt $ $Revision: 4 $
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:bs="http://schemas.microsoft.com/robotics/2007/07/lego/nxt/buttons.html">

  <xsl:import href="/resources/dss/Microsoft.Dss.Runtime.Home.MasterPage.xslt" />

  <xsl:template match="/">
    <xsl:call-template name="MasterPage">
      <xsl:with-param name="title">
        NXT Brick Buttons
      </xsl:with-param>
      <xsl:with-param name="serviceName">
        NXT Brick Buttons
      </xsl:with-param>
      <xsl:with-param name="description">
        Status of NXT Brick Buttons.
      </xsl:with-param>
      <xsl:with-param name="head">
        <script language="javascript" type="text/javascript" src="/resources/NxtBrick.Y2007.M07/Microsoft.Robotics.Services.Sample.Lego.Nxt.Transforms.Common.js"/>
        <script language="javascript" type="text/javascript">
          <xsl:text>
var pollingFrequency = </xsl:text>
          <xsl:value-of select="/bs:ButtonState/bs:PollingFrequencyMs"/>
          <xsl:text>;
var timeStamp = "</xsl:text>
          <xsl:value-of select="/bs:ButtonState/bs:Buttons/bs:TimeStamp"/>
          <xsl:text>";
</xsl:text>

          <![CDATA[

function onState(state)
{
    if (state.Buttons.TimeStamp != timeStamp)
    {
      timeStamp = state.Buttons.TimeStamp;
      document.all("timestampCell").innerText = timeStamp;

      handleButton(state.Buttons.PressedLeft == 'true', document.all("leftPressed"), document.all("leftCell"));
      handleButton(state.Buttons.PressedEnter == 'true', document.all("enterPressed"), document.all("enterCell"));
      handleButton(state.Buttons.PressedRight == 'true', document.all("rightPressed"), document.all("rightCell"));
      handleButton(state.Buttons.PressedCancel == 'true', document.all("cancelPressed"), document.all("cancelCell"));
    }
    readState(pollingFrequency);
}

function handleButton(isPressed, textCell, visualCell)
{
  if (isPressed)
  {
    textCell.innerText = "Pressed";
    visualCell.style.border = "solid 2px black";
  }
  else
  {
    textCell.innerText = "Released";
    visualCell.style.border = "solid 0px white";
  }
}

function startPolling()
{
  setStateCompletion("onState");
  readState(pollingFrequency);
}

setTimeout("startPolling()", 1000);

          ]]>
        </script>
      </xsl:with-param>
    </xsl:call-template>
  </xsl:template>

  <!--<xsl:output method="html"/>-->

  <xsl:template match="/bs:ButtonState">
    <img src="/resources/NxtBrick.Y2007.M07/Microsoft.Robotics.Services.Sample.Lego.Nxt.Resources.NxtButtons.Image.png"
         align="right" alt="Buttons" width="32px" height="32px"/>
    <xsl:apply-templates select="bs:Buttons"/>
  </xsl:template>

  <xsl:template match="bs:Buttons">
    <table width="320">
      <tr class="even">
        <th width="25%">Left:</th>
        <td id="leftPressed">
          <xsl:call-template name="boolToValue">
            <xsl:with-param name="bool" select="bs:PressedLeft"/>
            <xsl:with-param name="true" select="'Pressed'"/>
            <xsl:with-param name="false" select="'Released'"/>
          </xsl:call-template>
        </td>
      </tr>
      <tr class="odd">
        <th>Enter:</th>
        <td id="enterPressed">
          <xsl:call-template name="boolToValue">
            <xsl:with-param name="bool" select="bs:PressedEnter"/>
            <xsl:with-param name="true" select="'Pressed'"/>
            <xsl:with-param name="false" select="'Released'"/>
          </xsl:call-template>
        </td>
      </tr>
      <tr class="even">
        <th>Right:</th>
        <td id="rightPressed">
          <xsl:call-template name="boolToValue">
            <xsl:with-param name="bool" select="bs:PressedRight"/>
            <xsl:with-param name="true" select="'Pressed'"/>
            <xsl:with-param name="false" select="'Released'"/>
          </xsl:call-template>
        </td>
      </tr>
      <tr class="odd">
        <th>Cancel:</th>
        <td id="cancelPressed">
          <xsl:call-template name="boolToValue">
            <xsl:with-param name="bool" select="bs:PressedCancel"/>
            <xsl:with-param name="true" select="'Pressed'"/>
            <xsl:with-param name="false" select="'Released'"/>
          </xsl:call-template>
        </td>
      </tr>
      <tr class="even">
        <th>Timestamp:</th>
        <td id="timestampCell">
          <xsl:value-of select="bs:TimeStamp"/>
        </td>
      </tr>
      <tr>
        <td colspan="2" align="center">
          <xsl:variable name="leftColor">
            <xsl:call-template name="boolToValue">
              <xsl:with-param name="bool" select="bs:PressedLeft"/>
              <xsl:with-param name="true" select="'black'"/>
              <xsl:with-param name="false" select="'white'"/>
            </xsl:call-template>
          </xsl:variable>
          <xsl:variable name="enterColor">
            <xsl:call-template name="boolToValue">
              <xsl:with-param name="bool" select="bs:PressedEnter"/>
              <xsl:with-param name="true" select="'black'"/>
              <xsl:with-param name="false" select="'white'"/>
            </xsl:call-template>
          </xsl:variable>
          <xsl:variable name="rightColor">
            <xsl:call-template name="boolToValue">
              <xsl:with-param name="bool" select="bs:PressedRight"/>
              <xsl:with-param name="true" select="'black'"/>
              <xsl:with-param name="false" select="'white'"/>
            </xsl:call-template>
          </xsl:variable>
          <xsl:variable name="cancelColor">
            <xsl:call-template name="boolToValue">
              <xsl:with-param name="bool" select="bs:PressedCancel"/>
              <xsl:with-param name="true" select="'black'"/>
              <xsl:with-param name="false" select="'white'"/>
            </xsl:call-template>
          </xsl:variable>
          <table cellspacing="8">
            <tr>
              <td id="leftCell" style="font-size:32px;width:32px;height=32px;background-color:#CCC;text-align:center;border:solid 2px {$leftColor};overflow:hidden;">&lt;</td>
              <td id="enterCell" style="width:32px;height=32px;color:#FA4;background-color:#FA4;text-align:center;border:solid 2px {$enterColor};overflow:hidden;">.</td>
              <td id="rightCell" style="font-size:32px;width:32px;height=32px;background-color:#CCC;text-align:center;border:solid 2px {$rightColor};overflow:hidden;">&gt;</td>
            </tr>
            <tr>
              <td style="width:32px;height:16px;"></td>
              <td id="cancelCell" style="width:32px;height:16px;color:#888;background-color:#888;text-align:center;border:solid 2px {$cancelColor};overflow:hidden;">.</td>
              <td style="width:32px;height:10px;"></td>
            </tr>
          </table>
        </td>
      </tr>
    </table>
  </xsl:template>
  
  <xsl:template name="boolToValue">
    <xsl:param name="bool"/>
    <xsl:param name="true"/>
    <xsl:param name="false"/>

    <xsl:choose>
      <xsl:when test="$bool = 'true'">
        <xsl:value-of select="$true"/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="$false"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

</xsl:stylesheet>