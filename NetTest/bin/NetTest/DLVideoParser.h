//=======================================================================================
//                 �������������������רҵ��ͨ������⣬��ʡ���Ŀ���ʱ��
// 
//	Copyright:	Copyright (c) Peng Zhang 
//  ��Ȩ���У�	����������޹�˾ (QQ:52401692)
//
//              ������Ǹ�����Ϊ����ҵĿ��ʹ�ã����������ɡ���ѵ�ʹ�õ�����������ʾ����
//              Ҳ�ڴ��յ�������������ͽ��飬��ͬ�Ľ�������Ʒ
//              ���������ҵʹ�ã���ô����Ҫ��ϵ���������Ʒ����ҵ��Ȩ��
//              ���������������ʾ����Ĵ�����⹫���������Ĵ���ֻ�޸����û�����ʹ�á�
//        
//  �ٷ���վ��  http://www.dolit.cn http://blog.dolit.cn
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


//������Ϣ������������վ���У������ſ��û���ṩframeCount�����Ϣ��
// ��¼һ�������ļ�����Ϣ����ַ��ʱ������С��

#pragma pack (push, old_value)   // ����VC++�������ṹ�����ֽ���
#pragma pack (4)                 // ����Ϊ��һ�ֽڶ��롣
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

// ��¼һ�����͵��ļ���Ϣ������flv��mp4��hd2���������塢����ȣ�
#pragma pack (push, old_value)   // ����VC++�������ṹ�����ֽ���
#pragma pack (4)                 // ����Ϊ��һ�ֽڶ��롣
struct VideoInType
{	
    char     *  strType;         //������Ƶ������

    //��Ƶ�����з��˺ܶ�Σ����жε���Ϣ
    int         segCount;        // �ֶε���Ŀ
    VideoSeg *  segs;            // �ֶε�����

    VideoInType()
    {
        strType = NULL;
    }
};
#pragma pack (pop, old_value)  

// ��¼����������Ƶ������Ϣ

#pragma pack (push, old_value)       // ����VC++�������ṹ�����ֽ���
#pragma pack (4)                     // ����Ϊ��һ�ֽڶ��롣

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
	VideoSiteID   siteID;			// ����վ��ID
    UINT64	timeLength;				//ʱ��
    UINT64  frameCount;				//֡��
    UINT64  totalSize;              //�ܴ�С

    char  * vName;                  //��Ƶ����
    char  * tags;		    		//��ǩ

    //��Ƶ�����ж��ָ�ʽ��ÿ����Ƶ��������Ϣ
    int         streamCount;        // ��Ƶ����ĸ���
    VideoInType * streams;     	    // �������������

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
///  �����Ƕ����ṩ�Ľӿڣ�����DLVideo_Parse����Ҫ����DLVideo_FreeVideoResult
///  �ͷ�DLL�ڲ�������ڴ�
///
/// ==================================================================


// ����һ����ַ��������Ƶ�����ĵ�ַ��Ϣ
DLVideo_API HRESULT WINAPI DLVideo_Parse (
    const char  *           webUrl,         // ��Ƶ����ҳ��ĵ�ַ
	const char  *			userAgent,		// ��������������ҳ����Ҫʹ����ͬ��userAgent�������ߺ������ߣ���Ĭ��ʹ�õ�IE ��userAgent
    VideoResult **          pInfo           // ������Ƶ����ʵ��Ϣ������ַ�����Ҫ�����ͷš�
    );

// �ͷŴ�����һ���ڴ棬��ֹ�ڴ�й¶
DLVideo_API void WINAPI DLVideo_FreeVideoResult (VideoResult * pInfo);


// �����������к�
DLVideo_API void WINAPI DLVideo_SetAppSettings (ULONGLONG cert1, LPCSTR productNumber, ULONGLONG cert2, ULONGLONG cert3, ULONGLONG cert4);

// ��ȡ������
DLVideo_API LPCSTR WINAPI DLVideo_GetMachineCode ();


#endif