using System.ComponentModel.DataAnnotations;

public class ClientViewModel
{
    [Required(ErrorMessage = "El nombre es requerido")]
    public string Nombres { get; set; }

    [Required(ErrorMessage = "El primer apellido es requerido")]
    public string Primer_Apellido { get; set; }

    [Required(ErrorMessage = "El segundo apellido es requerido")]
    public string ClientSurname2 { get; set; }

    [Required(ErrorMessage = "El teléfono es requerido")]
    public string ClientPhone1 { get; set; }

    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "El email no es válido")]
    public string ClientEmail { get; set; }
}
