// ---------------- Usings ----------------

using System.Xml.Linq;

// ---------------- Addins ----------------

#addin nuget:?package=Cake.ArgumentBinder&version=0.6.0

// ---------------- Constants -----------------

const string splitTarget = "split";

string target = Argument( "target", splitTarget );

// ---------------- Classes -----------------

public class SplitConfig
{
    [FilePathArgument(
        "voice_macro_config",
        Description = "Path to the Voice Macro config path.",
        HasSecretValue = false,
        Required = true,
        MustExist = true,
        ArgumentSource = ArgumentSource.CommandLineThenEnvironmentVariable
    )]
    public FilePath ConfigPath { get; set; }

    [DirectoryPathArgument(
        "voice_macro_profile_backup_dir",
        Description = "Where to output the split-out Voice Macro configs.",
        HasSecretValue = false,
        Required = true,
        MustExist = false,
        ArgumentSource = ArgumentSource.CommandLineThenEnvironmentVariable
    )]
    public DirectoryPath OutputDir { get; set; }
}

// ---------------- Tasks -----------------

Task( splitTarget )
.Does(
    () =>
    {
        SplitConfig config = CreateFromArguments<SplitConfig>();
        EnsureDirectoryExists( config.OutputDir );

        XDocument doc = XDocument.Load( config.ConfigPath.ToString() );

        const string expectedRootName = "ArrayOfVoiceMacroProfile";
        if( expectedRootName.Equals( doc.Root.Name.LocalName ) == false )
        {
            throw new InvalidOperationException(
                $"Root XML node should be named '{expectedRootName}'.  Got: '{doc.Root.Name.LocalName}'"
            );
        }

        foreach( XElement profileNode in doc.Root.Elements() )
        {
            if( "VoiceMacroProfile".Equals( profileNode.Name.LocalName ) )
            {
                string name = "Untitled";
                foreach( XElement profileSetting in profileNode.Elements() )
                {
                    if( "ProfileName".Equals( profileSetting.Name.LocalName ) )
                    {
                        name = profileSetting.Value;
                    }
                }

                FilePath outputFile = config.OutputDir.CombineWithFilePath( File( name + ".xml" ) );
                XDocument outputDoc = new XDocument(
                    new XDeclaration( "1.0", "utf-8", "yes" ),
                    profileNode
                );
                outputDoc.Save( outputFile.ToString() );
            }
        }
    }
).DescriptionFromArguments<SplitConfig>( "Splits up the given VoiceMacro config into separate profiles that can be imported." );

// ---------------- Run  -----------------

RunTarget( target );