//=======================================================================================
//                 点量软件，致力于做最专业的通用软件库，节省您的开发时间
// 
//	Copyright:	Copyright (c) Peng Zhang 
//  版权所有：	点量软件有限公司 (QQ:52401692)
//
//              如果您是个人作为非商业目的使用，您可以自由、免费的使用点量软件库和演示程序，
//              也期待收到您反馈的意见和建议，共同改进点量产品
//              如果您是商业使用，那么您需要联系作者申请产品的商业授权。
//              点量软件库所有演示程序的代码对外公开，软件库的代码只限付费用户个人使用。
//        
//  官方网站：  http://www.dolit.cn http://blog.dolit.cn
//
//=======================================================================================  

#ifndef DOLIT_CN_VIDEO_PARSER_INC_H_INCLUDED
#define DOLIT_CN_VIDEO_PARSER_INC_H_INCLUDED

#pragma once

#ifdef DLVIDEOPARSER_EXPORTS 
#define DLVideo_API extern "C" _declspec(dllexport)
#pragma message("--- EXPORT DOLIT_VIDEO_PARSER Library  ...") 
#else      
#define DLVideo_API extern "C" _declspec(dllimport)
#pragma message("--- IMPORT DOLIT_VIDEO_PARSER Library  ...") 
#endif     


//以下信息并不是所有网站都有，比如优酷就没有提供frameCount这个信息。
// 记录一个单个文件的信息，网址和时长、大小等

#pragma pack (push, old_value)   // 保存VC++编译器结构对齐字节数
#pragma pack (4)                 // 设置为以一字节对齐。
struct VideoSeg
{
    UINT64 fileSize;
    int    seconds;
    int	   fileNO;
    char * url;

    VideoSeg()
    {
        fileSize = 0;
        seconds = fileNO = 0;
        url = NULL;
    }
};
#pragma pack (pop, old_value)  

// 记录一种类型的文件信息，比如flv、mp4、hd2……（高清、标清等）
#pragma pack (push, old_value)   // 保存VC++编译器结构对齐字节数
#pragma pack (4)                 // 设置为以一字节对齐。
struct VideoInType
{	
    char     *  strType;         //本组视频的类型

    //视频可能切分了很多段，所有段的信息
    int         segCount;        // 分段的数目
    VideoSeg *  segs;            // 分段的数组

    VideoInType()
    {
        strType = NULL;
    }
};
#pragma pack (pop, old_value)  

// 记录解析出的视频基本信息

#pragma pack (push, old_value)       // 保存VC++编译器结构对齐字节数
#pragma pack (4)                     // 设置为以一字节对齐。

enum VideoSiteID
{
	VSite_UnSupported = 0,
	VSite_Sina,
	VSite_Youku,
	VSite_Youtube,
	VSite_Tudou,
	VSite_Ku6,
	VSite_Umiwi,
	VSite_Sohu,
	VSite_QQ,
	VSite_Qiyi,
	VSite_Cntv,
	VSite_56,
	VSite_M1905,
    VSite_6CN,
    VSite_Joy,
    VSite_163,
    VSite_iFeng,
    VSite_LeTv,
};

struct VideoResult
{
	VideoSiteID   siteID;			// 该网站的ID
    UINT64	timeLength;				//时长
    UINT64  frameCount;				//帧数
    UINT64  totalSize;              //总大小

    char  * vName;                  //视频名字
    char  * tags;		    		//标签

    //视频可能有多种格式，每种视频的下载信息
    int         streamCount;        // 视频种类的个数
    VideoInType * streams;     	    // 返回种类的数组

    VideoResult()
    {
        timeLength = frameCount = totalSize = 0;
        vName = NULL;
        tags = NULL;  
        streamCount = 0;
		siteID = VSite_UnSupported;
    }
};
#pragma pack (pop, old_value)  

/// ==================================================================
///
///  以下是对外提供的接口，调用DLVideo_Parse后，需要调用DLVideo_FreeVideoResult
///  释放DLL内部分配的内存
///
/// ==================================================================


// 解析一个网址，返回视频真正的地址信息
DLVideo_API HRESULT WINAPI DLVideo_Parse (
    const char  *           webUrl,         // 视频所在页面的地址
	const char  *			userAgent,		// 土豆网等特殊网页，需要使用相同的userAgent（解析者和下载者）。默认使用的IE 的userAgent
    VideoResult **          pInfo           // 传出视频的真实信息，这块地址随后需要进行释放。
    );

// 释放传出的一块内存，防止内存泄露
DLVideo_API void WINAPI DLVideo_FreeVideoResult (VideoResult * pInfo);


// 设置正版序列号
DLVideo_API void WINAPI DLVideo_SetAppSettings (ULONGLONG cert1, LPCSTR productNumber, ULONGLONG cert2, ULONGLONG cert3, ULONGLONG cert4);

// 获取机器码
DLVideo_API LPCSTR WINAPI DLVideo_GetMachineCode ();


#endif