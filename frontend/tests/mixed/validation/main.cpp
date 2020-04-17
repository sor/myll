#include "main.h"
int main(int argc, const char *(argv)[])
{
    using namespace std;
    using namespace Rethmann::OOA::Uebung2_3;
    Stack<std::string> message;
    message.push( "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.\n" );
    message.push( "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.\n" );
    message.push( "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.\n" );
    message.push( "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.\n" );
    message.push( "\n" ).push( "!" ).push( "Myll" ).push( ", " ).push( "Hello" );
    while( !message.isEmpty() )
    {
        cout << message.top();
        message.pop();
    }
    cout.flush();
    return 0;
}
