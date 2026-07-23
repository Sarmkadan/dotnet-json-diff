import sys
import os

def build():
    print("Building the project...")
    # Add your build commands here
    print("Running dotnet build...")
    os.system("dotnet build")

if __name__ == "__main__":
    build()
