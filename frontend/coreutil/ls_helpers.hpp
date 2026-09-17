#pragma once

#include <sys/stat.h>
#include <pwd.h>
#include <grp.h>
#include <string>

namespace ls_helpers {

inline std::string fileTypeLetter( const std::string& path )
{
	struct stat st;
	if( stat( path.c_str(), &st ) != 0 )
		return "?";

	if( S_ISDIR( st.st_mode ) )  return "d";
	if( S_ISLNK( st.st_mode ) )  return "l";
	if( S_ISCHR( st.st_mode ) )  return "c";
	if( S_ISBLK( st.st_mode ) )  return "b";
	if( S_ISFIFO( st.st_mode ) ) return "p";
	if( S_ISSOCK( st.st_mode ) ) return "s";
	return "-";
}

inline std::string permissionString( const std::string& path )
{
	struct stat st;
	if( stat( path.c_str(), &st ) != 0 )
		return "---------";

	char rwx[] = "---------";
	mode_t m = st.st_mode;

	if( m & S_IRUSR ) rwx[0] = 'r';
	if( m & S_IWUSR ) rwx[1] = 'w';
	if( m & S_IXUSR ) rwx[2] = 'x';
	if( m & S_IRGRP ) rwx[3] = 'r';
	if( m & S_IWGRP ) rwx[4] = 'w';
	if( m & S_IXGRP ) rwx[5] = 'x';
	if( m & S_IROTH ) rwx[6] = 'r';
	if( m & S_IWOTH ) rwx[7] = 'w';
	if( m & S_IXOTH ) rwx[8] = 'x';

	return std::string( rwx );
}

inline unsigned long long hardLinkCount( const std::string& path )
{
	struct stat st;
	if( stat( path.c_str(), &st ) != 0 )
		return 0;
	return static_cast<unsigned long long>( st.st_nlink );
}

inline std::string ownerName( const std::string& path )
{
	struct stat st;
	if( stat( path.c_str(), &st ) != 0 )
		return "?";

	if( passwd* pw = getpwuid( st.st_uid ) )
		return pw->pw_name;

	return std::to_string( st.st_uid );
}

inline std::string groupName( const std::string& path )
{
	struct stat st;
	if( stat( path.c_str(), &st ) != 0 )
		return "?";

	if( group* gr = getgrgid( st.st_gid ) )
		return gr->gr_name;

	return std::to_string( st.st_gid );
}

inline std::string formatTime( const std::string& path )
{
	struct stat st;
	if( stat( path.c_str(), &st ) != 0 )
		return "?";

	char buf[64]{};
	struct tm* tm_info = localtime( &st.st_mtime );
	if( !tm_info )
		return "?";

	strftime( buf, sizeof( buf ), "%b %e %H:%M", tm_info );
	return std::string( buf );
}

} // namespace ls_helpers
